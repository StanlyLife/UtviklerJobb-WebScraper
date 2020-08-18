using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using web_scraper.Interfaces;
using web_scraper.models;

namespace web_scraper.Controllers {

	[Route("nav")]
	[ApiController]
	public class NavController : ControllerBase {
		private readonly INavApiRequest navApiRequest;

		public NavController(INavApiRequest navApiRequest) {
			this.navApiRequest = navApiRequest;
		}

		[HttpGet]
		public async Task<string> GetAsync() {
			List<JobModel> jobList = await navApiRequest.SendApiRequest();

			return JsonConvert.SerializeObject(jobList);
		}

		//public async Task<List<JobModel>> SendApiRequest() {
		//	client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("bearer", ApiKey);
		//	client.DefaultRequestHeaders.Add("accept", "application/json");
		//	HttpResponseMessage response = await client.GetAsync(url);

		//	var byteArray = response.Content.ReadAsByteArrayAsync().Result;
		//	var result = Encoding.UTF8.GetString(byteArray, 0, byteArray.Length);

		//	dynamic dynJson = JsonConvert.DeserializeObject(result) as Newtonsoft.Json.Linq.JObject;

		//	List<JobModel> jobList = new List<JobModel>();
		//	foreach (var item in dynJson["content"]) {
		//		jobList.Add(TransferJobModels(item));
		//	}

		//	return jobList;
		//}

		//public JobModel TransferJobModels(dynamic item) {
		//	JobModel job = new JobModel() {
		//		JobId = Guid.NewGuid().ToString(),
		//		OriginWebsite = "nav",
		//		/**/
		//		advertExpires = item["expires"],
		//		Accession = item["starttime"],
		//		Admissioner = item["employer"]["name"],
		//		AdmissionerDescription = item["employer"]["description"],
		//		AdmissionerWebsite = item["employer"]["homepage"],
		//		DescriptionHtml = item["description"],

		//		Deadline = item["applicationDue"],
		//		LocationCity = item["workLocations"][0]["city"],
		//		LocationAdress = item["workLocations"][0]["adress"],
		//		LocationZipCode = item["workLocations"][0]["postalCode"],
		//		Modified = item["updated"],
		//		NumberOfPositions = item["positioncount"],
		//		PositionTitle = item["title"],
		//		PositionType = item["engagementtype"],
		//		ForeignJobId = item["uuid"],
		//		Sector = item["sector"],
		//	};

		//	JobCategoryModel category1 = new JobCategoryModel() {
		//		Category = item["occupationCategories"][0]["level1"],
		//		JobId = job.JobId,
		//	};
		//	JobCategoryModel category2 = new JobCategoryModel() {
		//		Category = item["occupationCategories"][0]["level2"],
		//		JobId = job.JobId,
		//	};
		//	//Add category to db

		//	job.Admissioner = item["source"];
		//	if (job.Admissioner == "null" || string.IsNullOrWhiteSpace(job.Admissioner)) {
		//		job.Admissioner = "Nav";
		//	}

		//	job.AdvertUrl = item["title"];
		//	if (string.IsNullOrWhiteSpace(job.AdvertUrl)) {
		//		job.AdvertUrl = item["link"];
		//	}

		//	//Add job to db
		//	return job;
		//}
	}
}