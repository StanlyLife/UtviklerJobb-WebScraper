using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using website_scraper.Models;

namespace web_scraper.models {

	public class JobListModel {
		public IEnumerable<JobAdModel> shortJobList { get; set; }
		public IEnumerable<JobListingModel> descriptiveJobList { get; set; }
	}
}