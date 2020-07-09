using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using web_scraper.Data;
using web_scraper.models;

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
						where entity.AdvertId == AdvertId
						&&
						entity.tag.ToLower() == tag.ToLower()
						select entity;
			if (query.Count() > 0) {
				return true;
			}
			return false;
		}

		public Task Purge() {
			throw new NotImplementedException();
		}

		public bool SaveChanges() {
			if (db.SaveChanges() > 0) { return true; }
			return false;
		}

		void IJobTagHandler.Purge() {
			throw new NotImplementedException();
		}
	}
}