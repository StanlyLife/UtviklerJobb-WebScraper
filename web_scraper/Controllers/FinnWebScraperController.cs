using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System;
using Newtonsoft.Json;
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