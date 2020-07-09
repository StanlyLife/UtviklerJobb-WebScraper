using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using web_scraper.models;
using website_scraper.Models;

namespace web_scraper.Interfaces {

	public interface IJobHandler {

		bool SaveChanges();

		/*Purge Table*/

		void Purge();

		/*Create*/

		Task<JobAdModel> AddJobAd(JobAdModel job);

		Task<JobListingModel> AddJobListing(JobListingModel job);

		/*Read*/

		Task<JobListingModel> GetJobListingById(string id);

		Task<JobAdModel> GetJobAdById(string id);

		/*Delete*/

		Task<JobListingModel> DeleteJobListing(string id);

		Task<JobAdModel> DeleteJobAd(string id);
	}
}