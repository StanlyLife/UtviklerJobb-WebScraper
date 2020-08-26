using AngleSharp.Dom;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using web_scraper.models;

namespace web_scraper.Interfaces.JobRetrievers {
	/*
	*
	* TODO
	* - Add all categories ✔
	* - Add items to database ✔
	* - Check for duplicates system
	* - refactor code
	* - Add update database method: Check if already exist
	*
	*/

	public interface IJobregScraper {

		public Task<List<JobModel>> CheckForUpdates(bool checkCategories);
	}
}