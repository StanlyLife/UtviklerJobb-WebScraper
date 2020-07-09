using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using web_scraper.models;

namespace web_scraper.Interfaces {

	public interface IJobCategoryHandler {

		bool SaveChanges();

		/*Purge table*/

		void Purge();

		/*Create*/

		Task<JobCategoryModel> AddJobCategory(JobCategoryModel category);

		/*Read*/

		Task<bool> JobIdHasCategory(string AdvertId, string category);

		Task<JobTagsModel> GetJobCategoriesById(string AdvertId);

		/*Delete*/

		Task<JobTagsModel> DeleteJobCategoriesById(string AdvertId);
	}
}