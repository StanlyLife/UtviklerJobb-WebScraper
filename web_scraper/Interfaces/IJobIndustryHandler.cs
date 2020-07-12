using System.Collections.Generic;
using System.Threading.Tasks;
using web_scraper.models;

namespace web_scraper.Interfaces {

	public interface IJobIndustryHandler {

		bool SaveChanges();

		/*Purge table*/

		void Purge();

		/*Create*/

		Task<JobIndustryModel> AddJobIndustry(JobIndustryModel industry);

		/*Read*/

		Task<List<JobIndustryModel>> GetJobIndustriesById(string jobId);

		/*Delete*/

		JobIndustryModel DeleteJobIndustryById(JobIndustryModel industry);
	}
}