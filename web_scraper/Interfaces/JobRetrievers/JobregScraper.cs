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
using web_scraper.Interfaces.Database;
using web_scraper.Lists.Categories.jobreg;
using web_scraper.models;
using web_scraper.Services;

namespace web_scraper.Interfaces.JobRetrievers {

	public class JobregScraper : IJobregScraper {
		private readonly string websiteUrl = "https://www.jobreg.no/jobs.php?";
		private Stopwatch JobAdsTimer = new Stopwatch();
		private Stopwatch JobListingTimer = new Stopwatch();
		private readonly int GlobalMaxIteration = 15000;
		private readonly int MaxPagePerQuery = 500;
		private readonly IJobHandler jobHandler;
		private readonly IJobCategoryHandler jobCategoryHandler;
		private readonly IJobTagHandler jobTagHandler;
		private readonly IExistModified existModified;
		private int iteration = 0;
		private int currentPage = 1;

		public JobregScraper(
			IJobHandler jobHandler,
			IJobCategoryHandler jobCategoryHandler,
			IJobTagHandler jobTagHandler,
			IExistModified existModified
			) {
			this.jobHandler = jobHandler;
			this.jobCategoryHandler = jobCategoryHandler;
			this.jobTagHandler = jobTagHandler;
			this.existModified = existModified;
		}

		public async Task<List<JobModel>> CheckForUpdates(bool checkCategories) {
			List<JobModel> jobList = new List<JobModel>();
			//new DriverManager().SetUpDriver(new ChromeConfig(), "Latest", Architecture.X64);
			/*Config*/
			var chromeoptions = new ChromeOptions();
			var seleniumConfigService = new SeleniumConfigService();
			chromeoptions = seleniumConfigService.SetDefaultChromeConfig(chromeoptions);
			var driver = new ChromeDriver(@"C:\WebDrivers", chromeoptions);

			/**/
			if (checkCategories) {
				jobList = await GetJobsWithCategories(jobList, driver);
			} else {
				jobList = await GetJobs(websiteUrl, new List<JobModel>(), driver, new List<string>());
			}

			/**/
			jobList = await GetListingInfoAsync(jobList, driver);
			driver.Quit();
			return jobList;
		}

		private async Task<List<JobModel>> GetJobsWithCategories(List<JobModel> jobList, ChromeDriver driver) {
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
					jobList = await GetJobsFromCategories(jobList, driver, branch, category, categoryWebsiteUrl);
				}
			}

			return jobList;
		}

		private async Task<List<JobModel>> GetJobsFromCategories(List<JobModel> jobList, ChromeDriver driver, KeyValuePair<string, List<string>> branch, string category, string categoryWebsiteUrl) {
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
			return jobList;
		}

		private async Task<List<JobModel>> GetListingInfoAsync(List<JobModel> jobs, ChromeDriver driver) {
			var config = Configuration.Default.WithDefaultLoader();
			var context = BrowsingContext.New(config);

			List<JobModel> newJobs = new List<JobModel>();
			List<JobModel> jobsThatAlreadyExists = new List<JobModel>();

			foreach (var job in jobs) {
				if (existModified.CheckIfExists(job.ForeignJobId)) {
					jobsThatAlreadyExists.Add(job);
					continue;
				}

				JobModel newJob = job;

				var document = await context.OpenAsync(job.AdvertUrl);
				Console.WriteLine($"\nURL: {job.AdvertUrl}");
				/*Admissioner*/
				newJob = GetListingAdmissionerInfo(job, document);
				newJob = GetListingTableContent(job, document);
				await jobHandler.AddJobListing(newJob);
			}
			jobHandler.SaveChanges();

			foreach (var job in jobsThatAlreadyExists) {
				//Check if job is updated or not
				var document = await context.OpenAsync(job.AdvertUrl);
				var desc = document.QuerySelector(".showBody");
				if (desc != null) {
					job.DescriptionHtml = desc.ToHtml();
					if (existModified.CheckIfModified(job.ForeignJobId, job.DescriptionHtml)) {
						var updateJob = jobHandler.GetJobListingByForeignId(job.ForeignJobId);
						updateJob = GetListingAdmissionerInfo(updateJob, document);
						updateJob = GetListingTableContent(updateJob, document);
						jobHandler.UpdateJob(updateJob);
						jobHandler.SaveChanges();
					}
				}
			}

			//ToDo
			//	Add job to database
			jobHandler.SaveChanges();
			return jobs;
		}

		private JobModel GetListingTableContent(JobModel job, IDocument document) {
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
			return job;
		}

		private JobModel GetListingAdmissionerInfo(JobModel job, IDocument document) {
			var desc = document.QuerySelector(".showBody");
			if (desc != null) {
				job.DescriptionHtml = desc.ToHtml();
				//Check if job is in database and has not been modified.
				//if so do not add it to database
				job.Description = desc.TextContent;
			} else {
				Console.WriteLine($"No jobdescription found for positon: {job.AdvertUrl}");
			}

			var contactPerson = document.Body.SelectSingleNode("/html/body/div[1]/header/div[2]/div/div/div/div/div[1]/div/div[2]/div/div[4]/div[2]/div[2]/span[1]/b");
			if (contactPerson != null) {
				job.AdmissionerContactPerson = contactPerson.TextContent;
			}

			var contactPersonTelepone = document.Body.SelectSingleNode("/html/body/div[1]/header/div[2]/div/div/div/div/div[1]/div/div[2]/div/div[4]/div[2]/div[2]/span[3]");
			if (contactPersonTelepone != null) {
				job.AdmissionerContactPersonTelephone = contactPersonTelepone.TextContent;
			}
			/*Description*/
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
			return job;
		}

		private async Task<List<JobModel>> GetJobs(string url, List<JobModel> jobList, ChromeDriver driver, List<string> categoryQueryList) {
			//Check if max iterations are reached

			#region refactor? to GetJobsSetup()

			if (iteration >= GlobalMaxIteration || currentPage >= MaxPagePerQuery) {
				Console.WriteLine($"Max limit reached, iterations {iteration}/{GlobalMaxIteration} : {currentPage}/{MaxPagePerQuery}");
				return jobList;
			}
			//Setup selenium webdriver
			driver.Navigate().GoToUrl(url);
			WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(20));
			wait.Until(ExpectedConditions.ElementExists(By.XPath("/html/body/div/header/div/div/div/div[2]/b[2]")));

			/*
			 *Check if amount of jobs > 1
			 *Some categories are empty
			 *
			 */

			if (driver.FindElement(By.XPath("/html/body/div/header/div/div/div/div[2]/b[2]")).Text == "0") {
				return jobList;
			}
			iteration++;

			//Selenium wait
			wait.Until(ExpectedConditions.ElementExists(By.CssSelector(".job-item")));
			IReadOnlyList<IWebElement> jobAdverts = driver.FindElements(By.CssSelector(".job-item"));

			#endregion refactor? to GetJobsSetup()

			/*##################
			#                  #
			#  Start scraping  #
			#                  #
			##################*/

			JobregCategories jobregCategories = new JobregCategories();

			#region refactor? to "scrapeAds()"

			foreach (var jobAd in jobAdverts) {
				JobModel job = new JobModel() {
					OriginWebsite = "jobreg",
					JobId = Guid.NewGuid().ToString(),
					AdvertScrapeDate = DateTime.Now.ToString("MM/dd/yyyy"),
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
					advertUrl = jobAd.FindElement(By.CssSelector(".adLogo > a"));
				} catch (NoSuchElementException) {
					//Sometimes the listing image is missing or a video is displayed
					advertUrl = jobAd.FindElement(By.CssSelector(".description > a"));
				}
				if (advertUrl != null) {
					/**/
					job.AdvertUrl = advertUrl.GetAttribute("href");
					GetForeignJobId(job, job.AdvertUrl);
					//
					//ToDo
					//	Check for infinite iterations???

					/**/
					job = ScrapeJobAdInfo(jobAd, job);
					/**/
					jobList.Add(job);
					foreach (var category in categoryList) {
						if (!await jobCategoryHandler.JobIdHasCategory(category.JobId, category.Category)) {
							await jobCategoryHandler.AddJobCategory(category);
							jobCategoryHandler.SaveChanges();
						}
					}
				}
			}

			#endregion refactor? to "scrapeAds()"

			Console.WriteLine($"Found {jobAdverts.Count} jobs at page {currentPage}!");

			//The default amount of jobs = 15
			//If default amount of jobs < 15 it means it is the last page
			if (jobAdverts.Count < 15) {
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

		private string GetNextPageUrl(string result, ReadOnlyCollection<IWebElement> nextPageLink) {
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

		private string GenerateNextPageUrl(IWebElement nextPageLink) {
			var NextPageHref = nextPageLink.GetAttribute("href");
			NextPageHref = NextPageHref.Trim();
			int pFrom = NextPageHref.IndexOf("'") + "'".Length;
			int pTo = NextPageHref.LastIndexOf("'");
			string result = NextPageHref.Substring(pFrom, pTo - pFrom);
			result = result.Replace(" ", "%20");
			return result;
		}

		private JobModel ScrapeJobAdInfo(IWebElement jobItem, JobModel job) {
			var header = jobItem.FindElement(By.CssSelector("h6 > a")).Text;
			job.PositionHeadline = header;
			Console.WriteLine($"JOB: {header} \n");
			/**/
			//job.AdvertUrl = advertUrl;
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
			return job;
		}

		private void GetForeignJobId(JobModel job, string advertUrl) {
			var IdFrom = advertUrl.LastIndexOf("-") + "-".Length;
			int IdTo = advertUrl.LastIndexOf(".html");
			var foreignJobId = advertUrl.Substring(IdFrom, IdTo - IdFrom);
			job.ForeignJobId = foreignJobId;
		}
	}
}