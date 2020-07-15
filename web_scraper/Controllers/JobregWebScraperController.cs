using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Js;
using AngleSharp.XPath;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Razor.Language;
using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using web_scraper.models;

namespace web_scraper.Controllers {

	[ApiController]
	[Route("Jobreg")]
	public class JobregWebScraperController : ControllerBase {
		private readonly string websiteUrl = "https://www.jobreg.no/jobs.php?";
		private Stopwatch JobAdsTimer = new Stopwatch();
		private Stopwatch JobListingTimer = new Stopwatch();
		private readonly int GlobalMaxIteration = 15;
		private readonly int MaxPagePerQuery = 2;
		private int iteration = 0;
		private int currentPage = 1;

		/*
		 *
		 * Jobreg does not have categories publicly available.
		 * The only way to get categories without an API
		 * would be to manually visit every url parameter.
		 * Then match results to the category with lists or dictionaries.
		 *
		 */

		private Dictionary<string, List<string>> BranchLinkCategories = new Dictionary<string, List<string>>() {
			{ "&branchCategory[]=9", new List<string>() {
				"&branch[]=73",
				"&branch[]=71",
				"&branch[]=68",
				"&branch[]=70",
				"&branch[]=72",
				"&branch[]=69",
				"&branch[]=74" } },
		};

		private Dictionary<string, string> Categories = new Dictionary<string, string>() {
			{ "&branch[]=68", "Annet" },
			{ "&branch[]=71", "Applikasjonsutvikling" },
			{ "&branch[]=73", "Drift av applikasjoner og infrastruktur" },
			{ "&branch[]=70", "forretningsutvikling" },
			{ "&branch[]=72", "Salg og markedsføring" },
			{ "&branch[]=69", "Support" },
			{ "&branch[]=74", "Utvikling av infrastruktur og maskinvare" },
		};

		public async Task<string> GetAsync() {
			JobAdsTimer.Start();
			var result = await CheckForUpdates(true);
			JobAdsTimer.Stop();
			Console.WriteLine($"@@@@@ Finished with {result.Count} results! @@@@@");
			return JsonConvert.SerializeObject(result);
		}

		private async Task<List<JobModel>> CheckForUpdates(bool checkCategories) {
			List<JobModel> jobList = new List<JobModel>();
			/**/
			var chromeoptions = new ChromeOptions();
			chromeoptions.AddArguments(new List<string>() { "headless", "allow-running-insecure-content" });
			var driver = new ChromeDriver(chromeoptions);
			/**/
			JobAdsTimer.Start();
			if (checkCategories) {
				foreach (var branch in BranchLinkCategories) {
					foreach (var category in branch.Value) {
						var categoryWebsiteUrl = websiteUrl + branch.Key + category;
						var categoryName = Categories.Where(x => x.Key.Equals(category)).Select(x => x.Value).FirstOrDefault();
						if (Categories.ContainsKey(category)) {
							Console.WriteLine($"\nCategory: {Categories.GetValueOrDefault(category)}");
						}
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
				}
			} else {
				jobList = await GetJobs(websiteUrl, new List<JobModel>(), driver, new List<string>());
			}
			JobAdsTimer.Stop();
			/**/
			JobListingTimer.Start();
			jobList = await GetListingInfoAsync(jobList, driver);
			JobListingTimer.Stop();
			/**/
			Console.WriteLine($"@@@@@ JobAdsTimer Finished after {JobAdsTimer.Elapsed} seconds! @@@@@");
			Console.WriteLine($"@@@@@ JobAdsTimer Finished after {JobListingTimer.Elapsed} seconds! @@@@@");
			/**/
			return jobList;
		}

		private async Task<List<JobModel>> GetListingInfoAsync(List<JobModel> jobs, ChromeDriver driver) {
			var config = Configuration.Default.WithDefaultLoader();
			var context = BrowsingContext.New(config);

			foreach (var job in jobs) {
				var document = await context.OpenAsync(job.AdvertUrl);
				Console.WriteLine($"\nURL: {job.AdvertUrl}");
				/*Admissioner*/
				GetListingAdmissionerInfo(job, document);
				/*GetTableContent*/
				GetListingTableContent(job, document);
			}
			//ToDo
			//	Add job to database
			return jobs;
		}

		private static void GetListingTableContent(JobModel job, IDocument document) {
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

		private static void GetListingAdmissionerInfo(JobModel job, IDocument document) {
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
					JobTagsModel tagModel = new JobTagsModel() {
						JobId = job.JobId,
						Tag = tag.TextContent,
					};
					//ToDo
					//	Add tag to database
				}
			} else {
				Console.WriteLine($"No tags found for positon: {job.AdvertUrl}");
			}
		}

		private async Task<List<JobModel>> GetJobs(string url, List<JobModel> jobList, ChromeDriver driver, List<string> categoryQueryList) {
			if (iteration >= GlobalMaxIteration || currentPage >= MaxPagePerQuery) {
				Console.WriteLine("Max limit reached");
				return jobList;
			}
			iteration++;

			driver.Navigate().GoToUrl(url);
			WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(20));
			wait.Until(ExpectedConditions.ElementExists(By.CssSelector(".job-item")));
			IReadOnlyList<IWebElement> jobs = driver.FindElements(By.CssSelector(".job-item"));

			foreach (var jobItem in jobs) {
				JobModel job = new JobModel() {
					OriginWebsite = "jobreg",
					JobId = Guid.NewGuid().ToString(),
				};
				/**/
				//TODO
				// NULL ERROR HANDLING ON ADVERTURL
				IWebElement advertUrl;
				try {
					advertUrl = jobItem.FindElement(By.CssSelector(".adLogo > a"));
				} catch (NoSuchElementException) {
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
						Console.WriteLine($"@@@@@@ \nPossible honeypot at {websiteUrl} \ncurrent page {currentPage} \nduplicate jobId {job.ForeignJobId} \njob url {job.AdvertUrl} \n@@@@@@");
						return jobList;
					}
					jobList.Add(job);
				}
			}
			Console.WriteLine($"Found {jobs.Count} jobs at page {currentPage}!");

			if (jobs.Count < 15) {
				return jobList;
			}
			/*
			 * CHECK NEXT PAGE
			 */
			string NextPageUrlFormat = "https://www.jobreg.no/jobs.php?";
			string NextPageUrlPagePrefix = "&start=";
			string NextPageUrlCategories = "";
			string NextPageUrl = "";
			string result = "";
			//IWebElement nextPageLink = driver.FindElement(By.XPath("/html/body/div/main/section/div/div/div/div/div[2]/div/ul/li[11]/a"));
			var nextPageLink = driver.FindElementsByCssSelector("#pagination li");
			if (nextPageLink != null /* && content == " > "*/) {
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
				if (!string.IsNullOrWhiteSpace(result)) {
					NextPageUrl = url + NextPageUrlCategories + NextPageUrlPagePrefix + result;
					Console.WriteLine($"\nNext page URL: '{NextPageUrl}' \n");
				} else {
					Console.WriteLine("did not find ' > '");
					Console.WriteLine($"result: {result}");
				}
			}

			// If next page link is present recursively call the function again with the new url
			if (!String.IsNullOrEmpty(NextPageUrl)) {
				Console.WriteLine("checking next page: " + NextPageUrl);
				currentPage++;
				return await GetJobs(NextPageUrl, jobList, driver, categoryQueryList);
			}

			return jobList;
		}

		private static string GenerateNextPageUrl(IWebElement nextPageLink) {
			var NextPageHref = nextPageLink.GetAttribute("href");
			NextPageHref = NextPageHref.Trim();
			int pFrom = NextPageHref.IndexOf("'") + "'".Length;
			int pTo = NextPageHref.LastIndexOf("'");
			string result = NextPageHref.Substring(pFrom, pTo - pFrom);
			result = result.Replace(" ", "%20");
			return result;
		}

		private static void GetJobAdInfo(IWebElement jobItem, JobModel job, string advertUrl) {
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

		private static void GetForeignJobId(JobModel job, string advertUrl) {
			var IdFrom = advertUrl.LastIndexOf("-") + "-".Length;
			int IdTo = advertUrl.LastIndexOf(".html");
			var foreignJobId = advertUrl.Substring(IdFrom, IdTo - IdFrom);
			job.ForeignJobId = foreignJobId;
		}
	}
}