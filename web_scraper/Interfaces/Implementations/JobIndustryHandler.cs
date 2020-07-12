using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using web_scraper.Data;
using web_scraper.models;
using Z.EntityFramework.Plus;

namespace web_scraper.Interfaces.Implementations {

	public class JobIndustryHandler : IJobIndustryHandler {
		private readonly WebScraperContext db;

		public JobIndustryHandler(WebScraperContext db) {
			this.db = db;
		}

		public async Task<JobIndustryModel> AddJobIndustry(JobIndustryModel industry) {
			await db.JobIndustry.AddAsync(industry);
			return industry;
		}

		public JobIndustryModel DeleteJobIndustryById(JobIndustryModel industry) {
			db.JobIndustry.Remove(industry);
			return industry;
		}

		public async Task<List<JobIndustryModel>> GetJobIndustriesById(string jobId) {
			var query = from entity in db.JobIndustry
						where entity.JobId.Equals(jobId)
						select entity;
			return await query.ToListAsync();
		}

		public void Purge() {
			db.JobIndustry.Delete();
			SaveChanges();
		}

		public bool SaveChanges() {
			if (db.SaveChanges() > 0) { return true; }
			return false;
		}
	}
}