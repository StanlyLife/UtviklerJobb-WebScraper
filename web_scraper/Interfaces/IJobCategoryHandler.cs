using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using web_scraper.models;

namespace web_scraper.Interfaces {

	internal interface IJobCategoryHandler {
		/*Purge table*/

		Task Purge();

		/*Create*/

		Task<JobCategoryModel> AddJobCategory(JobCategoryModel category);

		/*Read*/

		Task<JobTagsModel> GetJobCategoriesById(string AdvertId);

		/*Delete*/

		Task<JobTagsModel> DeleteJobCategoriesById(string AdvertId);
	}
}