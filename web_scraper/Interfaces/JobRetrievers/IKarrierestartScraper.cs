using AngleSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using web_scraper.models;

namespace web_scraper.Interfaces.JobRetrievers {

	public interface IKarrierestartScraper {

		public Task<List<JobModel>> CheckForUpdates();

		public Task<List<JobModel>> GetJobs(string url, IBrowsingContext contextParameter, List<JobModel> jobList);

		public Task<List<JobModel>> GetPositionListing(List<JobModel> jobList, IBrowsingContext contextParameter);
	}
}