using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Js;
using AngleSharp.XPath;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Razor.Language;
using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using web_scraper.models;
using web_scraper.Lists.Categories;
using web_scraper.Lists.Categories.jobreg;
using web_scraper.Services;
using System.Collections.ObjectModel;
using web_scraper.Interfaces;
using web_scraper.Interfaces.Implementations;
using web_scraper.Interfaces.JobRetrievers;

namespace web_scraper.Controllers {

	[ApiController]
	[Route("Jobreg")]
	public class JobregWebScraperController : ControllerBase {
		private readonly IJobregScraper jobregScraper;

		public JobregWebScraperController(IJobregScraper jobregScraper) {
			this.jobregScraper = jobregScraper;
		}

		public async Task<string> GetAsync() {
			var result = await jobregScraper.CheckForUpdates(true);

			return JsonConvert.SerializeObject(result);
		}
	}
}