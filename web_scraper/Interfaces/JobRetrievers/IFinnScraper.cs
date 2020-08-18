using AngleSharp;
using AngleSharp.Dom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using web_scraper.models;

namespace web_scraper.Interfaces.JobRetrievers {

	public interface IFinnScraper {

		public Task<List<JobModel>> GetPosition(string url, List<JobModel> results);

		public Task<List<JobModel>> CheckForUpdates();

		public Task<List<JobModel>> GetPositionListing(List<JobModel> jobs);

		public void CheckAndGetPositionTitle(JobModel fullJob, IDocument document);

		public Task GetAdmissionerWebsite(IBrowsingContext context, JobModel fullJob, IDocument document);

		public void GetPositionTags(JobModel fullJob, IDocument document);

		public Task GetPositionInfo(JobModel fullJob, IDocument document);

		public Task AddIndustry(JobModel fullJob, IElement des);
	}
}