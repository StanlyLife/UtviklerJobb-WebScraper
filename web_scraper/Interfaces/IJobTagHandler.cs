using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using web_scraper.models;

namespace web_scraper.Interfaces {

	internal interface IJobTagHandler {
		/*Purge table*/

		Task Purge();

		/*Create*/

		Task<JobTagsModel> AddJobTag(JobTagsModel tag);

		/*Read*/

		Task<JobTagsModel> GetJobTagsById(string AdvertId);

		/*Delete*/

		Task<JobTagsModel> DeleteJobTagsById(string AdvertId);
	}
}