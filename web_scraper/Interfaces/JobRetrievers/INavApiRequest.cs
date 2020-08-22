using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using web_scraper.models;

namespace web_scraper.Interfaces {

	public interface INavApiRequest {

		public Task<List<JobModel>> SendApiRequest();

		public JobModel SetJobValues(JobModel job, dynamic item);

		string UrlConstructor(int adsPerPage, int page);

		public Task<JobModel> TransferJobModelsAsync(dynamic item, bool update);
	}
}