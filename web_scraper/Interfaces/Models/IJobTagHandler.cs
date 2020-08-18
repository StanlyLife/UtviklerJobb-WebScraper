using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using web_scraper.models;

namespace web_scraper.Interfaces {

	public interface IJobTagHandler {

		bool SaveChanges();

		/*Purge table*/

		void Purge();

		/*Create*/

		Task<JobTagsModel> AddJobTag(JobTagsModel tag);

		/*Read*/

		Task<JobTagsModel> GetJobTagsById(string AdvertId);

		Task<bool> JobIdHasTag(string AdvertId, string tag);

		/*Delete*/

		Task<JobTagsModel> DeleteJobTagsById(string AdvertId);
	}
}