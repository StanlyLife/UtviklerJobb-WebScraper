using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using AngleSharp;
using AngleSharp.Html.Parser;
using System.Collections.Generic;
using System.Threading.Tasks;
using website_scraper.Models;
using System.Text.RegularExpressions;
using System;

namespace web_scraper.Controllers {

	[ApiController]
	[Route("[controller]")]
	public class WebScraperController : ControllerBase {
		/**
		 * The website I'm scraping has data where the paths are relative
		 * so I need a base url set somewhere to build full url's
		 */
		private readonly string websiteUrl = "https://www.finn.no/job/fulltime/search.html?filters=&occupation=0.23&occupation=1.23.244&occupation=1.23.83&page=1&sort=1";
		private readonly ILogger<WebScraperController> _logger;

		// Constructor
		public WebScraperController(ILogger<WebScraperController> logger) {
			_logger = logger;
		}

		private async Task<List<JobModel>> GetPageData(string url, List<JobModel> results) {
			var config = Configuration.Default.WithDefaultLoader();
			var context = BrowsingContext.New(config);
			var document = await context.OpenAsync(url);

			// Debug
			//_logger.LogInformation(document.DocumentElement.OuterHtml);

			var advertrows = document.QuerySelectorAll(".ads__unit");

			foreach (var row in advertrows) {
				// Create a container object
				JobModel advert = new JobModel();

				// Use regex to get all the numbers from this string
				var position = row.QuerySelector(".ads__unit__content__keys");
				if (position != null) {
					advert.Position = position.TextContent;
					/*TODO Remove*/
					Console.WriteLine(advert.Position);
				} else {
					advert.Position = "null";
				}

				var imageUrl = row.QuerySelector(".img-format__img");
				if (position != null) {
					advert.ImageUrl = imageUrl.TextContent;
				} else {
					advert.ImageUrl = "";
				}

				var link = row.QuerySelector(".ads__unit__link");
				if (position != null) {
					advert.AdvertUrl = link.TextContent;
				} else {
					advert.AdvertUrl = "";
				}

				// regxMatches = Regex.Matches(row.QuerySelector(".year").TextContent, @"\d+");
				// uint.TryParse(string.Join("", regxMatches), out uint year);
				// advert.Year = year;

				// // Get the fuel type from the ad
				// advert.Fuel = row.QuerySelector(".fuel").TextContent[0];

				// // Make and model
				// advert.MakeAndModel = row.QuerySelector(".make_and_model > a").TextContent;

				// // Link to the advert
				// advert.AdvertUrl = websiteUrl + row.QuerySelector(".make_and_model > a").GetAttribute("Href");

				results.Add(advert);
			}

			// Check if a next page link is present
			string nextPageUrl = "";
			var nextPageLink = document.QuerySelector(".next-page > .item");
			if (nextPageLink != null) {
				nextPageUrl = websiteUrl + nextPageLink.GetAttribute("Href");
			}

			// If next page link is present recursively call the function again with the new url
			if (!String.IsNullOrEmpty(nextPageUrl)) {
				return await GetPageData(nextPageUrl, results);
			}

			return results;
		}

		private async void CheckForUpdates(string url, string mailTitle) {
			// We create the container for the data we want
			List<JobModel> adverts = new List<JobModel>();

			/**
			 * GetPageData will recursively fill the container with data
			 * and the await keyword guarantees that nothing else is done
			 * before that operation is complete.
			 */
			await GetPageData(url, adverts);

			// TODO: Diff the data
		}

		[HttpGet]
		public string Get() {
			CheckForUpdates(websiteUrl, "Web-Scraper updates");
			return "Hello";
		}
	}
}