using AngleSharp;
using AngleSharp.Dom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using web_scraper.models;

namespace web_scraper.Interfaces.JobRetrievers {

	public interface IFinnScraper {

		public Task<List<JobModel>> CheckForUpdates();
	}
}