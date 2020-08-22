using System;
using System.Linq;
using System.Threading.Tasks;
using web_scraper.Data;
using web_scraper.models;
using Z.EntityFramework.Plus;

namespace web_scraper.Interfaces.Implementations {

	public class JobCategoryHandler : IJobCategoryHandler {
		private readonly WebScraperContext db;

		public JobCategoryHandler(WebScraperContext db) {
			this.db = db;
		}

		public async Task<JobCategoryModel> AddJobCategory(JobCategoryModel category) {
			await db.JobCategories.AddAsync(category);
			return category;
		}

		public Task<JobTagsModel> DeleteJobCategoriesById(string AdvertId) {
			throw new NotImplementedException();
		}

		public Task<JobTagsModel> GetJobCategoriesById(string AdvertId) {
			throw new NotImplementedException();
		}

		public async Task<bool> JobIdHasCategory(string JobId, string category) {
			var query = from entity in db.JobCategories
						where entity.JobId.Equals(JobId)
						&&
						entity.Category.Equals(category)
						select entity;
			var test = query.Any();
			if (query.Any()) {
				return true;
			}
			return false;
		}

		public void Purge() {
			db.JobCategories.Delete();
			SaveChanges();
		}

		public bool SaveChanges() {
			if (db.SaveChanges() > 0) { return true; }
			return false;
		}
	}
}