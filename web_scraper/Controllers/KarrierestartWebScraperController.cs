using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.XPath;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.Extensions.Configuration;
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
		private readonly int GlobalMaxIteration = 1;
		private readonly IJobHandler jobHandler;
		private readonly IJobCategoryHandler jobCategoryHandler;
		private readonly IJobTagHandler jobTagHandler;
		private readonly IJobIndustryHandler jobIndustryHandler;
		private int iteration = 0;

		public KarrierestartWebScraperController(IJobHandler jobHandler, IJobCategoryHandler jobCategoryHandler, IJobTagHandler jobTagHandler, IJobIndustryHandler jobIndustryHandler) {
			this.jobHandler = jobHandler;
			this.jobCategoryHandler = jobCategoryHandler;
			this.jobTagHandler = jobTagHandler;
			this.jobIndustryHandler = jobIndustryHandler;
		}

		public async Task<string> GetAsync() {
			jobTagHandler.Purge();
			jobHandler.Purge();
			jobIndustryHandler.Purge();
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

			Debug.WriteLine("startet scraping ads");
			jobList = await GetJobs(url, context, jobList);
			//scrape info from ads
			Debug.WriteLine("startet scraping listings");
			jobList = await GetPositionListing(jobList, context);

			return jobList;
		}

		private async Task<List<JobModel>> GetJobs(string url, IBrowsingContext contextParameter, List<JobModel> jobList) {
			if (iteration >= GlobalMaxIteration) {
				Console.WriteLine($"Max limit reached, iterations {iteration}/{GlobalMaxIteration}");
				return jobList;
			} else {
				iteration++;
			}
			var document = await contextParameter.OpenAsync(url);
			var jobListings = document.QuerySelectorAll(".featured-wrap");
			var context = contextParameter;

			foreach (var jobAd in jobListings) {
				var jobTitle = jobAd.QuerySelector(".title");
				var advertUrl = jobAd.QuerySelector(".j-futured-right > a");
				var admissionerLogo = jobAd.QuerySelector(".logo");
				if (jobTitle == null || advertUrl == null) { continue; }
				JobModel job = new JobModel() {
					JobId = Guid.NewGuid().ToString(),
					OriginWebsite = "Karrierestart"
				};
				/**/
				job.ImageUrl = "https://karrierestart.no" + admissionerLogo.GetAttribute("src");
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
				await GetJobs(nextPageUrl, context, jobList);
			}

			return jobList;
		}

		private async Task<List<JobModel>> GetPositionListing(List<JobModel> jobList, IBrowsingContext contextParameter) {
			foreach (var job in jobList) {
				var document = await contextParameter.OpenAsync(job.AdvertUrl);
				Console.WriteLine($"scraping listing: {job.AdvertUrl}");

				var description = document.QuerySelector(".cp_vacancies > .jobad-info-block.p_fix");
				var shortDescription = document.QuerySelector(".cp_about_left_wrapper.dual-bullet-list");
				if (description == null) { Debug.WriteLine($"no description at listing: {job.AdvertUrl}"); continue; } else {
					job.Description = description.TextContent;
					job.DescriptionHtml = description.Html();
				}
				var bransjer = document.Body.SelectSingleNode("//*[@id=\"job-fact-block\"]/div/div[2]/table/tbody/tr[6]/td[3]/span/a");
				var fagOmrader = document.Body.SelectSingleNode("//*[@id=\"job-fact-block\"]/div/div[2]/table/tbody/tr[4]/td[3]/span/a");
				var stillingsTittel = document.Body.SelectSingleNode("//*[@id=\"job-fact-block\"]/div/div[2]/table/tbody/tr[4]/td[2]/span/a");
				var stillingsHeader = document.Body.SelectSingleNode("//*[@id=\"job-fact-block\"]/h2");
				var locationAdress = document.Body.SelectSingleNode("//*[@id=\"job-fact-block\"]/div/div[2]/table/tbody/tr[4]/td[1]/span/a");
				var deadline = document.QuerySelector(".jobad-deadline-date");
				var admissioner = document.QuerySelector(".head-blu-txt");
				var admissionerWebsite = document.QuerySelector(".ctrl-info");
				var tiltredelse = document.QuerySelector(".element-sec");
				var tags = document.QuerySelector(".txt.job-tags");
				var stillingsType = document.Body.SelectSingleNode("//*[@id=\"job-fact-block\"]/div/div[2]/table/tbody/tr[2]/td[2]/span");

				if (bransjer != null) {
					var bransjeList = bransjer.TextContent.Split("/");
					int counter = 0;
					foreach (string b in bransjeList) {
						JobCategoryModel category = new JobCategoryModel {
							JobId = job.JobId,
							Category = b.Trim()
						};
						await jobCategoryHandler.AddJobCategory(category);
						jobCategoryHandler.SaveChanges();
						counter++;
					}
				} else {
					Debug.WriteLine($"no bransje at {job.AdvertUrl}");
				}

				if (fagOmrader != null) {
					var fagOmraderList = fagOmrader.TextContent.Split("/");
					int counter = 0;
					foreach (string b in fagOmraderList) {
						JobIndustryModel industry = new JobIndustryModel {
							JobId = job.JobId,
							Industry = b.Trim()
						};
						await jobIndustryHandler.AddJobIndustry(industry);
						jobIndustryHandler.SaveChanges();
						counter++;
					}
				} else {
					Debug.WriteLine($"no fagOmrader at {job.AdvertUrl}");
				}

				if (tags != null) {
					foreach (var tag in tags.ChildNodes) {
						if (string.IsNullOrWhiteSpace(tag.TextContent)) { continue; }
						JobTagsModel t = new JobTagsModel {
							JobId = job.JobId,
							Tag = tag.TextContent.Trim(),
						};
						jobTagHandler.AddJobTag(t);
						jobTagHandler.SaveChanges();
					}
				} else {
					Debug.WriteLine($"no tags at {job.AdvertUrl}");
				}

				if (stillingsTittel != null) {
					job.PositionTitle = stillingsTittel.TextContent;
				}
				if (stillingsHeader != null) {
					job.PositionHeadline = stillingsHeader.TextContent;
				}
				if (locationAdress != null) {
					job.LocationAdress = locationAdress.TextContent;
				}
				if (deadline != null) {
					job.Deadline = deadline.TextContent;
				}
				if (admissionerWebsite != null) {
					job.AdmissionerWebsite = admissionerWebsite.TextContent;
				}
				if (shortDescription != null) {
					job.ShortDescription = shortDescription.TextContent;
				}
				if (tiltredelse != null) {
					job.Accession = tiltredelse.TextContent;
				}
				if (stillingsType != null) {
					job.PositionType = stillingsType.TextContent.Trim();
					
				}
				if (admissioner != null) {
					job.Admissioner = admissioner.TextContent;
				}
				await jobHandler.AddJobListing(job);
			}
			jobHandler.SaveChanges();
			return jobList;
		}
	}
}