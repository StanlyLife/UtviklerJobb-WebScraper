using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using web_scraper.Data;
using web_scraper.Interfaces.Database;
using web_scraper.models;

namespace web_scraper.Interfaces.Implementations {

	public class NavApiRequest : INavApiRequest {
		public static readonly HttpClient client = new HttpClient();
		private readonly IJobHandler jobHandler;
		private readonly IJobCategoryHandler jobCategoryHandler;
		private readonly IJobTagHandler jobTagHandler;
		private readonly IJobIndustryHandler jobIndustryHandler;
		private readonly IExistModified existModifiedHandler;

		//This key is public and is found at: https://github.com/navikt/pam-public-feed
		private string ApiKey = "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJzdWIiOiJwdWJsaWMudG9rZW4udjFAbmF2Lm5vIiwiYXVkIjoiZmVlZC1hcGktdjEiLCJpc3MiOiJuYXYubm8iLCJpYXQiOjE1NTc0NzM0MjJ9.jNGlLUF9HxoHo5JrQNMkweLj_91bgk97ZebLdfx3_UQ";

		private string baseUrl = "https://arbeidsplassen.nav.no/public-feed/api/v1/ads?";

		/*
		*
		* TODO
		* - Scrape all 5000 listings, not just the first 100
		*
		*/

		public NavApiRequest(
			IJobHandler jobHandler,
			IJobCategoryHandler jobCategoryHandler,
			IJobTagHandler jobTagHandler,
			IJobIndustryHandler jobIndustryHandler,
			IExistModified existModifiedHandler
			) {
			this.jobHandler = jobHandler;
			this.jobCategoryHandler = jobCategoryHandler;
			this.jobTagHandler = jobTagHandler;
			this.jobIndustryHandler = jobIndustryHandler;
			this.existModifiedHandler = existModifiedHandler;
		}

		public async Task<List<JobModel>> SendApiRequest() {
			jobHandler.Purge();
			client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("bearer", ApiKey);
			client.DefaultRequestHeaders.Add("accept", "application/json");
			HttpResponseMessage response = await client.GetAsync(UrlConstructor(150, 2));

			var byteArray = response.Content.ReadAsByteArrayAsync().Result;
			var result = Encoding.UTF8.GetString(byteArray, 0, byteArray.Length);

			dynamic dynJson = JsonConvert.DeserializeObject(result) as Newtonsoft.Json.Linq.JObject;

			List<JobModel> jobList = new List<JobModel>();
			foreach (var item in dynJson["content"]) {
				if (existModifiedHandler.CheckIfExists(Convert.ToString(item["uuid"]))) {
					if (existModifiedHandler.CheckIfModified(Convert.ToString(item["uuid"]), Convert.ToString(item["description"]))) {
						Debug.WriteLine("updated job");
						JobModel job = await TransferJobModelsAsync(item, true);
						jobList.Add(job);
					} else {
						Debug.WriteLine("Job exists and is not modified");
						continue;
					}
				} else {
					Debug.WriteLine("Scraping job");
					JobModel job = await TransferJobModelsAsync(item, false);
					jobList.Add(job);
				}
			}
			jobHandler.SaveChanges();
			return jobList;
		}

		private string size = "size=";
		private string parameterSpace = "&";
		private string page = "page=";

		//Max 100 results per page
		//Max 5000 most recent ads aviable
		public string UrlConstructor(int adsPerPage, int pageNumber) {
			var url = baseUrl + size + adsPerPage + parameterSpace + page + pageNumber;
			return url;
		}

		public JobModel SetJobValues(JobModel job, dynamic item) {
			job.OriginWebsite = "nav";
			/**/
			job.advertPublished = item["published"];
			job.AdvertExpires = item["expires"];
			job.Accession = item["starttime"];
			job.Admissioner = item["employer"]["name"];
			job.AdmissionerDescription = item["employer"]["description"];
			job.AdmissionerWebsite = item["employer"]["homepage"];
			job.DescriptionHtml = item["description"];
			/**/
			job.Deadline = item["applicationDue"];
			job.LocationCity = item["workLocations"][0]["city"];
			job.LocationAdress = item["workLocations"][0]["adress"];
			job.LocationZipCode = item["workLocations"][0]["postalCode"];
			job.AdvertModified = item["updated"];
			job.NumberOfPositions = item["positioncount"];
			job.PositionHeadline = item["title"];
			job.PositionTitle = item["jobtitle"];
			job.PositionType = item["engagementtype"];
			job.ForeignJobId = item["uuid"];
			job.Sector = item["sector"];

			return job;
		}

		public async Task<JobModel> TransferJobModelsAsync(dynamic item, bool update) {
			if (update) {
				JobModel jobToUpdate = jobHandler.GetJobListingByForeignId(Convert.ToString(item["uuid"]));
				if (jobToUpdate.JobId == null) { return new JobModel(); }
				jobToUpdate = SetJobValues(jobToUpdate, item);
				jobHandler.UpdateJob(jobToUpdate);

				return jobToUpdate;
			}

			JobModel job = new JobModel();
			job.JobId = Guid.NewGuid().ToString();
			job = SetJobValues(job, item);

			JobCategoryModel category1 = new JobCategoryModel() {
				Category = item["occupationCategories"][0]["level1"],
				JobId = job.JobId,
			};
			JobCategoryModel category2 = new JobCategoryModel() {
				Category = item["occupationCategories"][0]["level2"],
				JobId = job.JobId,
			};
			if (await jobCategoryHandler.JobIdHasCategory(category1.JobId, category1.Category)) {
				jobCategoryHandler.AddJobCategory(category1);
				jobCategoryHandler.SaveChanges();
			}
			if (await jobCategoryHandler.JobIdHasCategory(category2.JobId, category2.Category)) {
				jobCategoryHandler.AddJobCategory(category2);
				jobCategoryHandler.SaveChanges();
			}

			job.Admissioner = item["source"];
			if (job.Admissioner == "null" || string.IsNullOrWhiteSpace(job.Admissioner)) {
				job.Admissioner = "Nav";
			}

			job.AdvertUrl = item["sourceurl"];
			if (string.IsNullOrWhiteSpace(job.AdvertUrl)) {
				job.AdvertUrl = item["link"];
			}

			//Add job to db
			await jobHandler.AddJobListing(job);
			return job;
		}
	}
}