using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using AngleSharp;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using AngleSharp.Dom;
using web_scraper.models;
using Newtonsoft.Json;
using AngleSharp.XPath;
using web_scraper.Interfaces;
using web_scraper.Interfaces.Implementations;
using System.Text.RegularExpressions;
using web_scraper.Interfaces.JobRetrievers;

namespace web_scraper.Controllers {

	[ApiController]
	[Route("Finn")]
	public class FinnWebScraperController : ControllerBase {
		private readonly IFinnScraper finnScraper;

		public FinnWebScraperController(IFinnScraper finnScraper) {
			this.finnScraper = finnScraper;
		}

		[HttpGet]
		public async Task<string> GetAsync() {
			var result = await finnScraper.CheckForUpdates();
			Console.WriteLine($"@@@@@ Finished with {result.Count} results! @@@@@");
			return JsonConvert.SerializeObject(result);
		}
	}
}