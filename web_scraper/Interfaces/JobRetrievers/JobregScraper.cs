using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.XPath;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using web_scraper.Lists.Categories.jobreg;
using web_scraper.models;
using web_scraper.Services;

namespace web_scraper.Interfaces.JobRetrievers {

	public class JobregScraper : IJobregScraper {
		private readonly string websiteUrl = "https://www.jobreg.no/jobs.php?";
		private Stopwatch JobAdsTimer = new Stopwatch();
		private Stopwatch JobListingTimer = new Stopwatch();
		private readonly int GlobalMaxIteration = 15;
		private readonly int MaxPagePerQuery = 2;
		private readonly IJobHandler jobHandler;
		private readonly IJobCategoryHandler jobCategoryHandler;
		private readonly IJobTagHandler jobTagHandler;
		private int iteration = 0;
		private int currentPage = 1;

		public JobregScraper(IJobHandler jobHandler, IJobCategoryHandler jobCategoryHandler, IJobTagHandler jobTagHandler) {
			this.jobHandler = jobHandler;
			this.jobCategoryHandler = jobCategoryHandler;
			this.jobTagHandler = jobTagHandler;
		}

		public async Task<List<JobModel>> CheckForUpdates(bool checkCategories) {
			List<JobModel> jobList = new List<JobModel>();
			/**/
			var chromeoptions = new ChromeOptions();
			var seleniumConfigService = new SeleniumConfigService();
			chromeoptions = seleniumConfigService.SetDefaultChromeConfig(chromeoptions);
			var driver = new ChromeDriver(chromeoptions);
			/**/
			JobAdsTimer.Start();
			if (checkCategories) {
				JobregCategories jobregCategories = new JobregCategories();
				//Get parent category
				foreach (var branch in jobregCategories.BranchLinkCategories) {
					//Get sub category
					if (iteration >= GlobalMaxIteration) { break; };
					foreach (var category in branch.Value) {
						if (iteration >= GlobalMaxIteration) {
							Console.WriteLine($"Max limit reached, iterations {iteration}/{GlobalMaxIteration} : {currentPage}/{MaxPagePerQuery}");
							break;
						}
						var categoryWebsiteUrl = websiteUrl + branch.Key + category;
						Console.WriteLine($"\nCategory: {jobregCategories.Categories.GetValueOrDefault(category)}");
						await GetJobsFromCategories(jobList, driver, branch, category, categoryWebsiteUrl);
					}
				}
			} else {
				jobList = await GetJobs(websiteUrl, new List<JobModel>(), driver, new List<string>());
			}
			JobAdsTimer.Stop();
			/**/
			JobListingTimer.Start();
			jobList = await GetListingInfoAsync(jobList, driver);
			JobListingTimer.Stop();
			return jobList;
		}

		public async Task GetJobsFromCategories(List<JobModel> jobList, ChromeDriver driver, KeyValuePair<string, List<string>> branch, string category, string categoryWebsiteUrl) {
			/*Current category list*/
			var categoryQueryList = new List<string>();
			categoryQueryList.Add(branch.Key);
			categoryQueryList.Add(category);
			Console.WriteLine($"testing categortBranc {branch.Key} with category: {category}");
			/*execute scraper*/
			Console.WriteLine($"starting at {categoryWebsiteUrl}\n");
			currentPage = 1;
			List<JobModel> tempList = new List<JobModel>();
			tempList = await GetJobs(categoryWebsiteUrl, new List<JobModel>(), driver, categoryQueryList);
			Console.WriteLine($"@@@ Finished one category, found {tempList.Count()} jobs @@@");
			Console.WriteLine($"@@@ Finished one category, currently {jobList.Count()} jobs in joblist @@@");
			jobList.AddRange(tempList);
			Console.WriteLine($"@@@ Finished one category, added {tempList.Count()} jobs in joblist: {jobList.Count()} @@@");
		}

		public async Task<List<JobModel>> GetListingInfoAsync(List<JobModel> jobs, ChromeDriver driver) {
			var config = Configuration.Default.WithDefaultLoader();
			var context = BrowsingContext.New(config);

			foreach (var job in jobs) {
				var document = await context.OpenAsync(job.AdvertUrl);
				Console.WriteLine($"\nURL: {job.AdvertUrl}");
				/*Admissioner*/
				GetListingAdmissionerInfo(job, document);
				/*GetTableContent*/
				GetListingTableContent(job, document);
				jobHandler.AddJobListing(job);
			}
			//ToDo
			//	Add job to database
			jobHandler.SaveChanges();
			return jobs;
		}

		public void GetListingTableContent(JobModel job, IDocument document) {
			var tableInfoList = document.QuerySelectorAll("tr");
			foreach (var row in tableInfoList) {
				var title = row.QuerySelector(".table-info-title");
				var value = row.QuerySelector(".table-info-value");
				if (title != null && value != null) {
					switch (title.TextContent.ToLower()) {
						case "firma":
						job.Admissioner = value.TextContent;
						break;

						case "sted":
						job.LocationAdress = value.TextContent;
						break;

						case "by":
						job.LocationCity = value.TextContent;
						break;

						case "fylke":
						job.LocationCounty = value.TextContent;
						break;

						case "nettside":
						job.AdmissionerWebsite = value.GetAttribute("href");
						break;

						case "arbeidstittel":
						job.PositionTitle = value.TextContent;
						break;

						case "tiltredelse":
						job.Accession = value.TextContent;
						break;

						case "søknadsfrist":
						job.Deadline = value.TextContent;
						break;

						default:
						Console.WriteLine($"NULL {title.TextContent} -> {value.TextContent}");
						break;
					}
				} else {
					Console.WriteLine(row.ToHtml());
				}
			}
		}

		public void GetListingAdmissionerInfo(JobModel job, IDocument document) {
			var contactPerson = document.Body.SelectSingleNode("/html/body/div[1]/header/div[2]/div/div/div/div/div[1]/div/div[2]/div/div[4]/div[2]/div[2]/span[1]/b");
			if (contactPerson != null) {
				job.AdmissionerContactPerson = contactPerson.TextContent;
			}

			var contactPersonTelepone = document.Body.SelectSingleNode("/html/body/div[1]/header/div[2]/div/div/div/div/div[1]/div/div[2]/div/div[4]/div[2]/div[2]/span[3]");
			if (contactPersonTelepone != null) {
				job.AdmissionerContactPersonTelephone = contactPersonTelepone.TextContent;
			}
			/*Description*/
			var desc = document.QuerySelector(".showBody");
			if (desc != null) {
				job.DescriptionHtml = desc.ToHtml();
				job.Description = desc.TextContent;
			} else {
				Console.WriteLine($"No jobdescription found for positon: {job.AdvertUrl}");
			}
			/*tags*/
			var tags = document.QuerySelectorAll(".label-default");
			if (tags != null) {
				foreach (var tag in tags) {
					if (!string.IsNullOrWhiteSpace(tag.TextContent)) {
						JobTagsModel tagModel = new JobTagsModel() {
							JobId = job.JobId,
							Tag = tag.TextContent.Trim(),
						};
						jobTagHandler.AddJobTag(tagModel);
						jobTagHandler.SaveChanges();
					}
				}
			} else {
				Console.WriteLine($"No tags found for positon: {job.AdvertUrl}");
			}
		}

		public async Task<List<JobModel>> GetJobs(string url, List<JobModel> jobList, ChromeDriver driver, List<string> categoryQueryList) {
			if (iteration >= GlobalMaxIteration || currentPage >= MaxPagePerQuery) {
				Console.WriteLine($"Max limit reached, iterations {iteration}/{GlobalMaxIteration} : {currentPage}/{MaxPagePerQuery}");
				return jobList;
			}
			driver.Navigate().GoToUrl(url);
			WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(20));
			wait.Until(ExpectedConditions.ElementExists(By.XPath("/html/body/div/header/div/div/div/div[2]/b[2]")));
			if (driver.FindElement(By.XPath("/html/body/div/header/div/div/div/div[2]/b[2]")).Text == "0") {
				return jobList;
			}
			iteration++;
			wait.Until(ExpectedConditions.ElementExists(By.CssSelector(".job-item")));
			IReadOnlyList<IWebElement> jobs = driver.FindElements(By.CssSelector(".job-item"));
			JobregCategories jobregCategories = new JobregCategories();
			foreach (var jobItem in jobs) {
				JobModel job = new JobModel() {
					OriginWebsite = "jobreg",
					JobId = Guid.NewGuid().ToString(),
				};
				List<JobCategoryModel> categoryList = new List<JobCategoryModel>();

				for (int i = 0; i < categoryQueryList.Count(); i++) {
					JobCategoryModel temporaryCategory = new JobCategoryModel() {
						JobId = job.JobId,
					};
					if (i < 1) {
						temporaryCategory.Category = jobregCategories.BranchNames.GetValueOrDefault(categoryQueryList[i]);
					} else {
						temporaryCategory.Category = jobregCategories.Categories.GetValueOrDefault(categoryQueryList[i]);
					}
					categoryList.Add(temporaryCategory);
				}
				/**/
				IWebElement advertUrl;
				try {
					advertUrl = jobItem.FindElement(By.CssSelector(".adLogo > a"));
				} catch (NoSuchElementException) {
					//Sometimes the listing image is missing or a video is displayed
					advertUrl = jobItem.FindElement(By.CssSelector(".description > a"));
				}
				if (advertUrl != null) {
					/**/
					GetForeignJobId(job, advertUrl.GetAttribute("href"));
					/**/
					GetJobAdInfo(jobItem, job, advertUrl.GetAttribute("href"));
					/**/
					if (jobList.Contains(job)) {
						//Honeypot detection
						//TODO FIX
						Console.WriteLine($"@@@@@@ \nPossible honeypot at {websiteUrl} \ncurrent page {currentPage} \nduplicate jobId {job.ForeignJobId} \njob url {job.AdvertUrl} \n@@@@@@");
						return jobList;
					}
					jobList.Add(job);
					foreach (var category in categoryList) {
						jobCategoryHandler.AddJobCategory(category);
						jobCategoryHandler.SaveChanges();
					}
				}
			}
			Console.WriteLine($"Found {jobs.Count} jobs at page {currentPage}!");

			if (jobs.Count < 15) {
				//The default amount of jobs = 15
				//If default amount of jobs < 15 it means it is the last page
				return jobList;
			}

			/*
			 *
			 * CHECK NEXT PAGE
			 *
			 *
			 */
			string NextPageUrlFormat = "https://www.jobreg.no/jobs.php?";
			string NextPageUrlPagePrefix = "&start=";
			string NextPageUrlCategories = string.Empty;
			string NextPageUrl = string.Empty;
			string result = string.Empty;
			var nextPageLink = driver.FindElementsByCssSelector("#pagination li");
			if (nextPageLink != null) {
				result = GetNextPageUrl(result, nextPageLink);
				if (!string.IsNullOrWhiteSpace(result)) {
					NextPageUrl = url + NextPageUrlCategories + NextPageUrlPagePrefix + result;
					Debug.WriteLine($"\nNEXT PAGE URL: '{NextPageUrl}' \n");
					currentPage++;
					return await GetJobs(NextPageUrl, jobList, driver, categoryQueryList);
				}
				Debug.WriteLine("Unable to retrieve next page \n");
			}

			return jobList;
		}

		public string GetNextPageUrl(string result, ReadOnlyCollection<IWebElement> nextPageLink) {
			foreach (var next in nextPageLink) {
				if (next.Text.Trim() == (currentPage + 1).ToString() || next.Text == " > " || next.Text.Trim() == ">") {
					result = GenerateNextPageUrl(next.FindElement(By.CssSelector("a")));
					Console.WriteLine("FOUND IT! " + next.Text);
					if (!string.IsNullOrEmpty(result)) {
						break;
					} else {
						Console.WriteLine($"Next page href is null!");
					}
				} else {
					Console.WriteLine($" #pagination li = '{next.Text}' --- next page == {currentPage + 1 }");
				}
			}

			return result;
		}

		public string GenerateNextPageUrl(IWebElement nextPageLink) {
			var NextPageHref = nextPageLink.GetAttribute("href");
			NextPageHref = NextPageHref.Trim();
			int pFrom = NextPageHref.IndexOf("'") + "'".Length;
			int pTo = NextPageHref.LastIndexOf("'");
			string result = NextPageHref.Substring(pFrom, pTo - pFrom);
			result = result.Replace(" ", "%20");
			return result;
		}

		public void GetJobAdInfo(IWebElement jobItem, JobModel job, string advertUrl) {
			var header = jobItem.FindElement(By.CssSelector("h6 > a")).Text;
			job.PositionHeadline = header;
			Console.WriteLine($"JOB: {header} \n");
			/**/
			job.AdvertUrl = advertUrl;
			/**/
			var shortDesc = jobItem.FindElement(By.CssSelector(".description > .adBodySmall")).Text;
			job.ShortDescription = shortDesc;
			/**/
			var positionAmount = jobItem.FindElement(By.CssSelector(".about > .positions")).Text;
			job.NumberOfPositions = positionAmount;
			/**/
			var positionType = jobItem.FindElement(By.CssSelector(".about > .type")).Text;
			job.PositionType = positionType;
			/**/
			try {
				var imageUrl = jobItem.FindElement(By.CssSelector(".adLogo img")).GetAttribute("src");
				job.ImageUrl = imageUrl;
			} catch (NoSuchElementException) {
				Console.WriteLine($"No image for job ad at : {job.AdvertUrl}");
			}
		}

		public void GetForeignJobId(JobModel job, string advertUrl) {
			var IdFrom = advertUrl.LastIndexOf("-") + "-".Length;
			int IdTo = advertUrl.LastIndexOf(".html");
			var foreignJobId = advertUrl.Substring(IdFrom, IdTo - IdFrom);
			job.ForeignJobId = foreignJobId;
		}
	}
}