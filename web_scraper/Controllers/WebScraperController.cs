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

		public WebScraperController(ILogger<WebScraperController> logger) {
			_logger = logger;
		}

		private async Task<List<JobModel>> GetPageData(string url, List<JobModel> results) {
			var config = Configuration.Default.WithDefaultLoader();
			var context = BrowsingContext.New(config);
			var document = await context.OpenAsync(url);

			// Debug
			//_logger.LogInformation(document.DocumentElement.OuterHtml);

			//var advertrows = document.QuerySelectorAll(".ads__unit");

			if (document.QuerySelectorAll("article") != null) {
				var advertrows = document.QuerySelectorAll("article");

				foreach (var row in advertrows) {
					// Create a container object
					JobModel advert = new JobModel();

					var position = row.QuerySelector(".ads__unit__content__keys");
					var info = row.QuerySelector(".ads__unit__link");
					if (position != null) {
						//Executing this if statement together with the one above causes
						//NullPointerException
						if (string.IsNullOrWhiteSpace(position.TextContent)) {
							advert.Position = info.TextContent;
						}
						advert.Position = position.TextContent;
						advert.ShortDescription = info.TextContent;
						Console.WriteLine(advert.Position);
					} else {
						advert.Position = info.TextContent;
					}

					var imageUrl = row.QuerySelector(".img-format__img");
					advert.ImageUrl = imageUrl.GetAttribute("src");

					advert.AdvertUrl = "https://finn.no" + info.GetAttribute("href");

					var contentList = row.QuerySelectorAll(".ads__unit__content__list");
					try {
						advert.Admissioner = contentList[0].TextContent;
						advert.NumberOfPositions = contentList[1].TextContent;
					} catch (Exception e) {
						Console.WriteLine(e);
					}

					results.Add(advert);
				}
			}

			// Check if a next page link is present
			string nextPageUrl = "https://www.finn.no/job/fulltime/search.html";
			var nextPageLink = document.QuerySelector(".button--icon-right");
			if (nextPageLink != null) {
				nextPageUrl = nextPageUrl + nextPageLink.GetAttribute("href");
				Console.WriteLine("\n neste side funnet!");
				Console.WriteLine(nextPageLink.GetAttribute("href") + "\n");
			} else {
				Console.WriteLine("\nFINISHED\n");
				nextPageUrl = "";
			}

			// If next page link is present recursively call the function again with the new url
			if (!String.IsNullOrEmpty(nextPageUrl)) {
				Console.WriteLine("checking next page: " + nextPageUrl);
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
			adverts = await GetPageData(url, adverts);
			int stillinger = 0;
			int NullStillinger = 0;
			foreach (var jobb in adverts) {
				if (!string.IsNullOrWhiteSpace(jobb.Position)) {
					Console.WriteLine("\n");
					Console.WriteLine($"tittel: {jobb.Position}");
					Console.WriteLine($"ShortDescription: {jobb.ShortDescription}");
					Console.WriteLine($"bilde: {jobb.ImageUrl}");
					Console.WriteLine($"url: {jobb.AdvertUrl}");
					Console.WriteLine($"positions: {jobb.NumberOfPositions}");
					Console.WriteLine($"employer: {jobb.Admissioner}");
					stillinger++;
				} else {
					Console.WriteLine("\n@@@@@@@@@@@@@@@@@@@@@@@");
					Console.WriteLine($"tittel: {jobb.Position}");
					Console.WriteLine($"ShortDescription: {jobb.ShortDescription}");
					Console.WriteLine($"positions: {jobb.NumberOfPositions}");
					Console.WriteLine($"employer: {jobb.Admissioner}");
					Console.WriteLine("@@@@@@@@@@@@@@@@@@@@@@@");
					NullStillinger++;
				}
			}
			Console.WriteLine($"\nStillinger: {stillinger}");
			Console.WriteLine($"\nNull Stillinger: {NullStillinger}");
			// TODO: Diff the data
		}

		[HttpGet]
		public string Get() {
			CheckForUpdates(websiteUrl, "Web-Scraper updates");
			return "Hello";
		}
	}
}