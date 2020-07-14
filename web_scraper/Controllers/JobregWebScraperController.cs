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
		private readonly string websiteUrl = "https://www.jobreg.no/jobs.php";
		private readonly int maxIterations = 2;
		private Stopwatch JobAdsTimer = new Stopwatch();
		private Stopwatch JobListingTimer = new Stopwatch();
		private int iteration = 0;

		public async Task<string> GetAsync() {
			JobAdsTimer.Start();
			var result = await CheckForUpdates();
			JobAdsTimer.Stop();
			Console.WriteLine($"@@@@@ Finished with {result.Count} results! @@@@@");
			return JsonConvert.SerializeObject(result);
		}

		private async Task<List<JobModel>> CheckForUpdates() {
			List<JobModel> jobList = new List<JobModel>();
			/**/
			var chromeoptions = new ChromeOptions();
			chromeoptions.AddArguments(new List<string>() { "headless", "allow-running-insecure-content" });
			var driver = new ChromeDriver(chromeoptions);
			/**/
			JobAdsTimer.Start();
			jobList = await GetJobs(websiteUrl, jobList, driver);
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
					Console.WriteLine($"No jobs found for positon: {job.AdvertUrl}");
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

				/*GetTableContent*/
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
			//ToDo
			//	Add job to database
			return jobs;
		}

		private async Task<List<JobModel>> GetJobs(string url, List<JobModel> jobList, ChromeDriver driver) {
			if (iteration >= maxIterations) {
				return jobList;
			}
			iteration++;

			driver.Navigate().GoToUrl(url);
			WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(20));
			wait.Until(ExpectedConditions.ElementExists(By.CssSelector(".job-item")));
			IReadOnlyList<IWebElement> jobs = driver.FindElements(By.CssSelector(".job-item"));

			foreach (var jobItem in jobs) {
				JobModel job = new JobModel() {
					OriginWebsite = "jobreg.no",
					JobId = Guid.NewGuid().ToString(),
				};
				var header = jobItem.FindElement(By.CssSelector("h6 > a")).Text;
				Console.WriteLine($"JOB: {header} \n");
				job.PositionHeadline = header;
				/**/
				var advertUrl = jobItem.FindElement(By.CssSelector(".adLogo > a")).GetAttribute("href");
				/**/
				Console.WriteLine(advertUrl);
				job.AdvertUrl = advertUrl;
				/**/
				var IdFrom = advertUrl.LastIndexOf("-") + "-".Length;
				int IdTo = advertUrl.LastIndexOf(".html");
				var foreignJobId = advertUrl.Substring(IdFrom, IdTo - IdFrom);
				Console.WriteLine($"JobID --- {foreignJobId}");
				job.ForeignJobId = foreignJobId;
				/**/
				var shortDesc = jobItem.FindElement(By.CssSelector(".description > .adBodySmall")).Text;
				Console.WriteLine($"DESCRIPTION: {shortDesc}");
				job.ShortDescription = shortDesc;
				/**/
				var positionAmount = jobItem.FindElement(By.CssSelector(".about > .positions")).Text;
				Console.WriteLine($"Positions: {positionAmount}");
				job.NumberOfPositions = positionAmount;
				/**/
				var positionType = jobItem.FindElement(By.CssSelector(".about > .type")).Text;
				Console.WriteLine($"Position Type: {positionType}");
				job.PositionType = positionType;
				/**/
				var imageUrl = jobItem.FindElement(By.CssSelector(".adLogo img")).GetAttribute("src");
				Console.WriteLine($"PositionLogo: {imageUrl}");
				job.ImageUrl = imageUrl;
				/**/
				jobList.Add(job);
			}
			Console.WriteLine($"Found {jobs.Count} Jobs!");
			/*
			 *
			 * CHECK NEXT PAGE
			 *
			 */
			string NextPageUrlFormat = "https://www.jobreg.no/jobs.php?start=";
			string NextPageUrl = "";
			IWebElement nextPageLink = driver.FindElement(By.XPath("/html/body/div/main/section/div/div/div/div/div[2]/div/ul/li[11]/a"));
			if (nextPageLink != null) {
				var NextPageHref = nextPageLink.GetAttribute("href");
				NextPageHref = NextPageHref.Trim();
				int pFrom = NextPageHref.IndexOf("'") + "'".Length;
				int pTo = NextPageHref.LastIndexOf("'");
				string result = NextPageHref.Substring(pFrom, pTo - pFrom);
				result = result.Replace(" ", "%20");
				NextPageUrl = NextPageUrlFormat + result;
			} else {
				Console.WriteLine("\nFINISHED\n");
				NextPageUrlFormat = "";
			}

			// If next page link is present recursively call the function again with the new url
			if (!String.IsNullOrEmpty(NextPageUrl)) {
				Console.WriteLine("checking next page: " + NextPageUrlFormat);
				return await GetJobs(NextPageUrl, jobList, driver);
			}

			return jobList;
		}
	}
}