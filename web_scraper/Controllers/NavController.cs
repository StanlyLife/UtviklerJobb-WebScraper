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
using web_scraper.models;

namespace web_scraper.Controllers {

	[Route("nav")]
	[ApiController]
	public class NavController : ControllerBase {
		private static readonly HttpClient client = new HttpClient();

		//This key is public and is found at: https://github.com/navikt/pam-public-feed
		public string ApiKey = "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJzdWIiOiJwdWJsaWMudG9rZW4udjFAbmF2Lm5vIiwiYXVkIjoiZmVlZC1hcGktdjEiLCJpc3MiOiJuYXYubm8iLCJpYXQiOjE1NTc0NzM0MjJ9.jNGlLUF9HxoHo5JrQNMkweLj_91bgk97ZebLdfx3_UQ";

		public string url = "https://arbeidsplassen.nav.no/public-feed/api/v1/ads?page=1&size=5";

		public NavController() {
		}

		[HttpGet]
		public async Task<string> GetAsync() {
			client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("bearer", ApiKey);
			client.DefaultRequestHeaders.Add("accept", "application/json");
			HttpResponseMessage response = await client.GetAsync(url);

			var byteArray = response.Content.ReadAsByteArrayAsync().Result;
			var result = Encoding.UTF8.GetString(byteArray, 0, byteArray.Length);

			dynamic dynJson = JsonConvert.DeserializeObject(result) as Newtonsoft.Json.Linq.JObject;

			List<JobModel> jobList = new List<JobModel>();
			foreach (var item in dynJson["content"]) {
				jobList.Add(GetJobs(item));
			}

			return JsonConvert.SerializeObject(jobList);
		}

		public JobModel GetJobs(dynamic item) {
			JobModel job = new JobModel() {
				JobId = Guid.NewGuid().ToString(),
				ForeignJobId = item["uuid"],
				PositionType = item["engagementtype"],
				NumberOfPositions = item["positioncount"],
				Modified = item["updated"],
				PositionTitle = item["title"],
				AdvertUrl = item["title"],
				Admissioner = item["employer"]["name"],
				AdmissionerWebsite = item["employer"]["homepage"],
				Sector = item["sector"],
				Accession = item["starttime"],
				Deadline = item["applicationDue"]
			};
			//JobCategoryModel category1 = new JobCategoryModel() {
			//	Category = item["occupationCategories"]["level1"],
			//	JobId = job.JobId,
			//};
			//JobCategoryModel category2 = new JobCategoryModel() {
			//	Category = item["occupationCategories"]["level1"],
			//	JobId = job.JobId,
			//};

			//Console.WriteLine($"Category1: {category1.Category}, Category2: {category2.Category}");

			return job;
		}
	}
}