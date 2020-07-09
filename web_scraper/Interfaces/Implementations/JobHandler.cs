using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using web_scraper.Data;
using web_scraper.models;
using website_scraper.Models;

namespace web_scraper.Interfaces.Implementations {

	public class JobHandler : IJobHandler {
		private readonly WebScraperContext db;

		public JobHandler(WebScraperContext db) {
			this.db = db;
		}

		public async Task<JobAdModel> AddJobAd(JobAdModel job) {
			await db.Jobs.AddAsync(job);
			return job;
		}

		public Task<JobListingModel> AddJobListing(JobListingModel job) {
			throw new NotImplementedException();
		}

		public Task<JobAdModel> DeleteJobAd(string id) {
			throw new NotImplementedException();
		}

		public Task<JobListingModel> DeleteJobListing(string id) {
			throw new NotImplementedException();
		}

		public Task<JobAdModel> GetJobAdById(string id) {
			throw new NotImplementedException();
		}

		public Task<JobListingModel> GetJobListingById(string id) {
			throw new NotImplementedException();
		}

		public void Purge() {
			var query = from entity in db.Jobs
						where entity.AdvertId != null
						select entity;
			foreach (var row in query) {
				db.Jobs.Remove(row);
			}
			SaveChanges();
		}

		public bool SaveChanges() {
			if (db.SaveChanges() > 0) { return true; }
			return false;
		}
	}
}