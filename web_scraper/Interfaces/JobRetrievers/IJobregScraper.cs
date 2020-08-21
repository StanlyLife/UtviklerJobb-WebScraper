using AngleSharp.Dom;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using web_scraper.models;

namespace web_scraper.Interfaces.JobRetrievers {
	/*
	*
	* TODO
	* - Add all categories ✔
	* - Add items to database ✔
	* - Check for duplicates system
	* - Rework honeypot detector
	* - refactor code
	* - Add update database method: Check if already exist
	*
	*/

	public interface IJobregScraper {

		public Task<List<JobModel>> CheckForUpdates(bool checkCategories);

		public Task GetJobsFromCategories(List<JobModel> jobList, ChromeDriver driver, KeyValuePair<string, List<string>> branch, string category, string categoryWebsiteUrl);

		public Task<List<JobModel>> GetListingInfoAsync(List<JobModel> jobs, ChromeDriver driver);

		public void GetListingTableContent(JobModel job, IDocument document);

		public void GetListingAdmissionerInfo(JobModel job, IDocument document);

		public Task<List<JobModel>> GetJobs(string url, List<JobModel> jobList, ChromeDriver driver, List<string> categoryQueryList);

		public string GetNextPageUrl(string result, ReadOnlyCollection<IWebElement> nextPageLink);

		public string GenerateNextPageUrl(IWebElement nextPageLink);

		public void GetJobAdInfo(IWebElement jobItem, JobModel job, string advertUrl);

		public void GetForeignJobId(JobModel job, string advertUrl);
	}
}