using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using web_scraper.models;

namespace web_scraper.Interfaces {

	public interface IJobHandler {
		/*Create*/

		Task<JobModel> AddJobListing(JobModel job);

		/*Read*/

		JobModel GetJobListingById(string id);

		JobModel GetJobListingByForeignId(string id);

		/*Update*/

		bool SaveChanges();

		JobModel UpdateJob(JobModel job);

		/*Delete*/

		void Purge();

		Task<JobModel> DeleteJobListing(string id);
	}
}