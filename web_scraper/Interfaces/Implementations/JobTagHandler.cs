using System;
using System.Linq;
using System.Threading.Tasks;
using web_scraper.Data;
using web_scraper.models;
using Z.EntityFramework.Plus;

namespace web_scraper.Interfaces.Implementations {

	public class JobTagHandler : IJobTagHandler {
		private readonly WebScraperContext db;

		public JobTagHandler(WebScraperContext db) {
			this.db = db;
		}

		public async Task<JobTagsModel> AddJobTag(JobTagsModel tag) {
			await db.JobTags.AddAsync(tag);
			return tag;
		}

		public Task<JobTagsModel> DeleteJobTagsById(string AdvertId) {
			throw new NotImplementedException();
		}

		public Task<JobTagsModel> GetJobTagsById(string AdvertId) {
			throw new NotImplementedException();
		}

		public async Task<bool> JobIdHasTag(string AdvertId, string tag) {
			var query = from entity in db.JobTags
						where entity.JobId == AdvertId
						&&
						entity.tag.ToLower() == tag.ToLower()
						select entity;
			if (query.Count() > 0) {
				return true;
			}
			return false;
		}

		public void Purge() {
			db.JobTags.Delete();
			SaveChanges();
		}

		public bool SaveChanges() {
			if (db.SaveChanges() > 0) { return true; }
			return false;
		}
	}
}