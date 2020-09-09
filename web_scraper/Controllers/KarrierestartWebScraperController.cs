using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.XPath;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using web_scraper.Interfaces;
using web_scraper.Interfaces.JobRetrievers;
using web_scraper.models;

namespace web_scraper.Controllers {

	[ApiController]
	[Route("ks")]
	public class KarrierestartWebScraperController : ControllerBase {
		private readonly IKarrierestartScraper karrierestartScraper;

		public KarrierestartWebScraperController(IKarrierestartScraper karrierestartScraper) {
			this.karrierestartScraper = karrierestartScraper;
		}

		public async Task<string> GetAsync() {
			var result = await karrierestartScraper.CheckForUpdates();
			Console.WriteLine("finished");
			return JsonConvert.SerializeObject(result);
		}
	}
}