using System;
using System.Linq;
using System.Threading.Tasks;
using web_scraper.Data;
using web_scraper.models;
using Z.EntityFramework.Plus;

namespace web_scraper.Interfaces.Implementations {

	public class JobHandler : IJobHandler {
		private readonly WebScraperContext db;

		public JobHandler(WebScraperContext db) {
			this.db = db;
		}

		public async Task<JobModel> AddJobListing(JobModel job) {
			await db.JobListings.AddAsync(job);
			return job;
		}

		public Task<JobModel> DeleteJobListing(string id) {
			throw new NotImplementedException();
		}

		public JobModel GetJobListingById(string id) {
			var query = from entity in db.JobListings
						where entity.JobId.Equals(id)
						select entity;
			if (query.Any()) {
				return query.First();
			} else {
				Console.WriteLine($"did not find job with ID {id}");
				return new JobModel();
			}
		}

		public void Purge() {
			db.JobListings.Delete();
			SaveChanges();
		}

		public bool SaveChanges() {
			if (db.SaveChanges() > 0) { return true; }
			return false;
		}

		public JobModel UpdateJob(JobModel job) {
			var entity = db.JobListings.Attach(job);
			entity.State = Microsoft.EntityFrameworkCore.EntityState.Modified;
			SaveChanges();
			return job;
		}
	}
}