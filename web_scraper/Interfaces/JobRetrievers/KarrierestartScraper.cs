using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.XPath;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using web_scraper.Interfaces.Database;
using web_scraper.models;

namespace web_scraper.Interfaces.JobRetrievers {

	public class KarrierestartScraper : IKarrierestartScraper {
		private readonly string websiteUrl = "https://karrierestart.no/jobb?page=1";
		private Stopwatch JobAdsTimer = new Stopwatch();
		private Stopwatch JobListingTimer = new Stopwatch();
		private readonly int GlobalMaxIteration = 1;
		private readonly IJobHandler jobHandler;
		private readonly IJobCategoryHandler jobCategoryHandler;
		private readonly IJobTagHandler jobTagHandler;
		private readonly IJobIndustryHandler jobIndustryHandler;
		private readonly IExistModified existModified;
		private int iteration = 0;

		public KarrierestartScraper(
			IJobHandler jobHandler,
			IJobCategoryHandler jobCategoryHandler,
			IJobTagHandler jobTagHandler,
			IJobIndustryHandler jobIndustryHandler,
			IExistModified existModified) {
			this.jobHandler = jobHandler;
			this.jobCategoryHandler = jobCategoryHandler;
			this.jobTagHandler = jobTagHandler;
			this.jobIndustryHandler = jobIndustryHandler;
			this.existModified = existModified;
		}

		public async Task<List<JobModel>> CheckForUpdates() {
			List<JobModel> jobList = new List<JobModel>();
			/**/
			var config = Configuration.Default.WithDefaultLoader();
			var context = BrowsingContext.New(config);
			var url = websiteUrl;
			/**/

			Debug.WriteLine("startet scraping ads");
			jobList = await GetJobs(url, context, jobList);
			//scrape info from ads
			Debug.WriteLine("startet scraping listings");
			jobList = await GetPositionListing(jobList, context);

			return jobList;
		}

		public async Task<List<JobModel>> GetJobs(string url, IBrowsingContext contextParameter, List<JobModel> jobList) {
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
					OriginWebsite = "Karrierestart",
					AdvertScrapeDate = DateTime.Now.ToString("MM/dd/yyyy"),
				};
				/**/
				job.ForeignJobId = advertUrl.GetAttribute("href").Substring(advertUrl.GetAttribute("href").LastIndexOf('/') + 1);
				if (existModified.CheckIfExists(job.ForeignJobId)) {
					continue;
				}
				Console.WriteLine($"Foreign jobID: {job.ForeignJobId}");
				/**/
				job.ImageUrl = "https://karrierestart.no" + admissionerLogo.GetAttribute("src");
				/**/
				job.PositionHeadline = jobTitle.TextContent;
				/**/
				job.AdvertUrl = advertUrl.GetAttribute("href");
				/**/

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

		public async Task<List<JobModel>> GetPositionListing(List<JobModel> jobList, IBrowsingContext contextParameter) {
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
						await jobTagHandler.AddJobTag(t);
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