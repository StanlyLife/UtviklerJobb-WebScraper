using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using web_scraper.models;

namespace web_scraper.Interfaces {

	public interface INavApiRequest {

		public Task<List<JobModel>> SendApiRequest();

		public JobModel TransferJobModels(dynamic item);
	}
}