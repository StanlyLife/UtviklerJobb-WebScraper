using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.XPath;
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
			List<JobModel> jobList = new List<JobModel>();
			/**/
			var config = Configuration.Default.WithDefaultLoader();
			var context = BrowsingContext.New(config);
			/**/

			jobList = await GetJobs(url, context, jobList);
			//scrape info from ads
			await GetPositionListing(jobList, context);

			return new List<JobModel>();
		}

		private async Task<List<JobModel>> GetJobs(string url, IBrowsingContext contextParameter, List<JobModel> jobList) {
			if (iteration >= GlobalMaxIteration) {
				Console.WriteLine($"Max limit reached, iterations {iteration}/{GlobalMaxIteration}");
				return jobList;
			}
			var document = await contextParameter.OpenAsync(url);
			var jobListings = document.QuerySelectorAll(".featured-wrap");
			var context = contextParameter;

			foreach (var jobAd in jobListings) {
				var jobTitle = jobAd.QuerySelector(".title");
				var advertUrl = jobAd.QuerySelector(".j-futured-right > a");
				if (jobTitle == null || advertUrl == null) { continue; }
				JobModel job = new JobModel() {
					JobId = Guid.NewGuid().ToString(),
					OriginWebsite = "Karrierestart"
				};

				/**/
				job.PositionHeadline = jobTitle.TextContent;
				/**/
				job.AdvertUrl = advertUrl.GetAttribute("href");
				/**/
				job.ForeignJobId = advertUrl.GetAttribute("href").Substring(advertUrl.GetAttribute("href").LastIndexOf('/') + 1);
				Console.WriteLine($"Foreign jobID: {job.ForeignJobId}");

				Console.WriteLine(job.PositionHeadline);
				Console.WriteLine(job.AdvertUrl);

				/*END*/
				jobList.Add(job);
			}

			//Check if next page exists
			//Get Next page url

			var nextPageElement = document.QuerySelector(".next-pager-btn > a");
			var nextPageUrl = "";
			if (nextPageElement != null) {
				Console.WriteLine($"nextpage href = {nextPageElement.GetAttribute("href")}");
				nextPageUrl = "https://karrierestart.no" + nextPageElement.GetAttribute("href");
			}
			//Recursion
			if (!string.IsNullOrEmpty(nextPageUrl)) {
				GetJobs(nextPageUrl, context, jobList);
			}

			return jobList;
		}

		private async Task<List<JobModel>> GetPositionListing(List<JobModel> jobList, IBrowsingContext contextParameter) {
			foreach (var job in jobList) {
				var document = await contextParameter.OpenAsync(job.AdvertUrl);
			}

			return jobList;
		}
	}