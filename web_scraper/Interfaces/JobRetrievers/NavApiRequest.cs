using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using web_scraper.models;

namespace web_scraper.Interfaces.Implementations {

	public class NavApiRequest : INavApiRequest {
		public static readonly HttpClient client = new HttpClient();

		//This key is public and is found at: https://github.com/navikt/pam-public-feed
		public string ApiKey = "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJzdWIiOiJwdWJsaWMudG9rZW4udjFAbmF2Lm5vIiwiYXVkIjoiZmVlZC1hcGktdjEiLCJpc3MiOiJuYXYubm8iLCJpYXQiOjE1NTc0NzM0MjJ9.jNGlLUF9HxoHo5JrQNMkweLj_91bgk97ZebLdfx3_UQ";

		public string url = "https://arbeidsplassen.nav.no/public-feed/api/v1/ads?page=1&size=50";

		public async Task<List<JobModel>> SendApiRequest() {
			client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("bearer", ApiKey);
			client.DefaultRequestHeaders.Add("accept", "application/json");
			HttpResponseMessage response = await client.GetAsync(url);

			var byteArray = response.Content.ReadAsByteArrayAsync().Result;
			var result = Encoding.UTF8.GetString(byteArray, 0, byteArray.Length);

			dynamic dynJson = JsonConvert.DeserializeObject(result) as Newtonsoft.Json.Linq.JObject;

			List<JobModel> jobList = new List<JobModel>();
			foreach (var item in dynJson["content"]) {
				jobList.Add(TransferJobModels(item));
			}

			return jobList;
		}

		public JobModel TransferJobModels(dynamic item) {
			JobModel job = new JobModel() {
				JobId = Guid.NewGuid().ToString(),
				OriginWebsite = "nav",
				/**/
				advertExpires = item["expires"],
				Accession = item["starttime"],
				Admissioner = item["employer"]["name"],
				AdmissionerDescription = item["employer"]["description"],
				AdmissionerWebsite = item["employer"]["homepage"],
				DescriptionHtml = item["description"],

				Deadline = item["applicationDue"],
				LocationCity = item["workLocations"][0]["city"],
				LocationAdress = item["workLocations"][0]["adress"],
				LocationZipCode = item["workLocations"][0]["postalCode"],
				Modified = item["updated"],
				NumberOfPositions = item["positioncount"],
				PositionTitle = item["title"],
				PositionType = item["engagementtype"],
				ForeignJobId = item["uuid"],
				Sector = item["sector"],
			};

			JobCategoryModel category1 = new JobCategoryModel() {
				Category = item["occupationCategories"][0]["level1"],
				JobId = job.JobId,
			};
			JobCategoryModel category2 = new JobCategoryModel() {
				Category = item["occupationCategories"][0]["level2"],
				JobId = job.JobId,
			};
			//Add category to db

			job.Admissioner = item["source"];
			if (job.Admissioner == "null" || string.IsNullOrWhiteSpace(job.Admissioner)) {
				job.Admissioner = "Nav";
			}

			job.AdvertUrl = item["title"];
			if (string.IsNullOrWhiteSpace(job.AdvertUrl)) {
				job.AdvertUrl = item["link"];
			}

			//Add job to db
			return job;
		}
	}
}