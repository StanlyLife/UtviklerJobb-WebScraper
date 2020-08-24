using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using web_scraper.Data;

namespace web_scraper.Interfaces.Database {

	public class ExistModified : IExistModified {
		private readonly WebScraperContext db;

		public ExistModified(WebScraperContext db) {
			this.db = db;
		}

		//returns true if job exists in database
		public bool CheckIfExists(string foreignId) {
			var query = from entity in db.JobListings
						where entity.ForeignJobId.Equals(foreignId)
						select entity;
			if (query.Any()) {
				return true;
			}
			Debug.WriteLine($"Joblisting with foreignkey {foreignId} - Nav - does not exist in context");
			return false;
		}

		//returns true if job.descriptionHtml != descriptionHtml
		public bool CheckIfModified(string foreignId, string descriptionHtml) {
			var query = from entity in db.JobListings
						where
						entity.ForeignJobId.Equals(foreignId)
						&&
						entity.DescriptionHtml.Equals(descriptionHtml)
						select entity;
			if (query.Any()) {
				return false;
			}
			Debug.WriteLine($"Joblisting with foreignkey {foreignId} - is modified!");
			return true;
		}
	}
}