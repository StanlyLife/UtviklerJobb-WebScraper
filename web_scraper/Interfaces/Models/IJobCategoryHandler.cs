using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using web_scraper.models;

namespace web_scraper.Interfaces {

	public interface IJobCategoryHandler {

		bool SaveChanges();

		void Purge();

		Task<JobCategoryModel> AddJobCategory(JobCategoryModel category);

		/*
		 * Returns TRUE if job has category
		 */

		Task<bool> JobIdHasCategory(string AdvertId, string category);

		Task<JobTagsModel> GetJobCategoriesById(string AdvertId);

		Task<JobTagsModel> DeleteJobCategoriesById(string AdvertId);
	}
}