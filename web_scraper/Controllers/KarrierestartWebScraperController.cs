using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using web_scraper.Interfaces;
using web_scraper.models;

namespace web_scraper.Controllers {

	[ApiController]
	[Route("ks")]
	public class KarrierestartWebScraperController : ControllerBase {
		private readonly string websiteUrl = "https://karrierestart.no/jobb?page=1";
		private Stopwatch JobAdsTimer = new Stopwatch();
		private Stopwatch JobListingTimer = new Stopwatch();
		private readonly int GlobalMaxIteration = 15;
		private readonly IJobHandler jobHandler;
		private readonly IJobCategoryHandler jobCategoryHandler;
		private readonly IJobTagHandler jobTagHandler;
		private int iteration = 0;
		private int currentPage = 1;

		public KarrierestartWebScraperController(IJobHandler jobHandler, IJobCategoryHandler jobCategoryHandler, IJobTagHandler jobTagHandler) {
			this.jobHandler = jobHandler;
			this.jobCategoryHandler = jobCategoryHandler;
			this.jobTagHandler = jobTagHandler;
		}

		public async Task<string> GetAsync() {
			jobTagHandler.Purge();
			jobHandler.Purge();
			jobCategoryHandler.Purge();
			/**/
			var result = await CheckForUpdates(websiteUrl);
			//Console.WriteLine($"@@@@@ Finished with {result.Count} results! @@@@@");
			/**/
			//Console.WriteLine($"@@@@@ JobAdsTimer Finished after {JobAdsTimer.Elapsed} seconds! @@@@@");
			//Console.WriteLine($"@@@@@ JobAdsTimer Finished after {JobListingTimer.Elapsed} seconds! @@@@@");
			/**/
			return JsonConvert.SerializeObject(result);
		}

		private async Task<List<JobModel>> CheckForUpdates(string url) {
			/**/
			var config = Configuration.Default.WithDefaultLoader();
			var context = BrowsingContext.New(config);
			/**/

			await GetJobs(url, context);

			return new List<JobModel>();
		}

		private async Task<List<JobModel>> GetJobs(string url, IBrowsingContext context) {
			var document = await context.OpenAsync(url);
			var jobListings = document.QuerySelectorAll(".featured-wrap");

			foreach (var jobAd in jobListings) {
				var jobTitle = jobAd.QuerySelector(".title");
				Console.WriteLine(jobTitle.TextContent);
			}

			//Check if next page exists
			//Get Next page url
			//Recursion

			return new List<JobModel>();
		}
	}
}