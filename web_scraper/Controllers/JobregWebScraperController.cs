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

		private Stopwatch sw = new Stopwatch();
		private int iteration = 0;

		public async Task<string> GetAsync() {
			sw.Start();
			var result = await CheckForUpdates();
			sw.Stop();
			Console.WriteLine($"@@@@@ Finished with {result.Count} results! @@@@@");
			Console.WriteLine($"@@@@@ Finished after {sw.Elapsed} seconds! @@@@@");
			return JsonConvert.SerializeObject(result);
		}

		private async Task<List<JobModel>> CheckForUpdates() {
			List<JobModel> jobList = new List<JobModel>();

			var chromeoptions = new ChromeOptions();
			chromeoptions.AddArguments(new List<string>() { "headless", "allow-running-insecure-content" });
			var driver = new ChromeDriver(chromeoptions);

			jobList = await GetJobs(websiteUrl, jobList, driver);
			return jobList;
		}

		private async Task<List<JobModel>> GetJobs(string url, List<JobModel> jobList, ChromeDriver driver) {
			//var config = Configuration.Default.WithDefaultLoader().WithJs();
			//var context = BrowsingContext.New(config);
			//var document = await context.OpenAsync(websiteUrl).WaitUntilAvailable();
			if (iteration >= 5) {
				return new List<JobModel>();
			}
			iteration++;

			driver.Navigate().GoToUrl(url);
			WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(20));
			wait.Until(ExpectedConditions.ElementToBeClickable(By.CssSelector(".job-item")));
			IReadOnlyList<IWebElement> jobs = driver.FindElements(By.CssSelector(".job-item"));

			foreach (var jobItem in jobs) {
				var header = jobItem.FindElement(By.CssSelector("h6 > a"));
				Console.WriteLine($"JOB: {header.Text} \n");
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
				return await GetJobs(NextPageUrl, new List<JobModel>(), driver);
			}

			return new List<JobModel>();
		}
	}
}