using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using AngleSharp;
using AngleSharp.Html.Parser;
using System.Collections.Generic;
using System.Threading.Tasks;
using website_scraper.Models;
using System.Text.RegularExpressions;
using System;
using AngleSharp.Dom;
using web_scraper.models;
using Newtonsoft.Json;
using System.Linq;
using AngleSharp.XPath;

namespace web_scraper.Controllers {

	[ApiController]
	[Route("Finn")]
	public class FinnWebScraperController : ControllerBase {
		/**
		 * The website I'm scraping has data where the paths are relative
		 * so I need a base url set somewhere to build full url's
		 */
		private readonly string websiteUrl = "https://www.finn.no/job/fulltime/search.html?filters=&occupation=0.23&occupation=1.23.244&occupation=1.23.83&page=1&sort=1";
		private readonly ILogger<FinnWebScraperController> _logger;

		public FinnWebScraperController(ILogger<FinnWebScraperController> logger) {
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
					advert.AdvertId = Guid.NewGuid().ToString();

					var position = row.QuerySelector(".ads__unit__content__keys");
					var info = row.QuerySelector(".ads__unit__link");
					if (position != null) {
						//Executing this if statement together with the one above causes
						//NullPointerException
						if (string.IsNullOrWhiteSpace(position.TextContent)) {
							advert.Position = info.TextContent;
						} else {
							advert.Position = position.TextContent;
							advert.ShortDescription = info.TextContent;
						}
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
				//return await GetPageData(nextPageUrl, results);
			}

			return results;
		}

		private async Task<JobListModel> CheckForUpdates(string url, string mailTitle) {
			// We create the container for the data we want
			List<JobModel> adverts = new List<JobModel>();
			List<DescriptiveJobModel> advertsInfo = new List<DescriptiveJobModel>();

			/**
			 * GetPageData will recursively fill the container with data
			 * and the await keyword guarantees that nothing else is done
			 * before that operation is complete.
			 */
			adverts = await GetPageData(url, adverts);
			PrintInfo(adverts);

			advertsInfo = await FillDescriptiveJobModelAsync(adverts);

			return new JobListModel {
				shortJobList = adverts,
				descriptiveJobList = advertsInfo
			};
		}

		private static async Task<List<DescriptiveJobModel>> FillDescriptiveJobModelAsync(List<JobModel> jobs) {
			var config = Configuration.Default.WithDefaultLoader();
			var context = BrowsingContext.New(config);
			List<DescriptiveJobModel> fullJobList = new List<DescriptiveJobModel>();
			foreach (var job in jobs) {
				DescriptiveJobModel fullJob = new DescriptiveJobModel {
					AdvertId = job.AdvertId
				};
				var document = await context.OpenAsync(job.AdvertUrl);
				//ToDo
				//Remove "<p><br /></p>"
				//Remove "<p>&nbsp;</p>"
				//Error catching
				fullJob.DescriptionHtml = document.QuerySelector(".import-decoration").ToHtml();
				//fullJob.DescriptionHtml.Replace(@"\n", " ");
				//fullJob.DescriptionHtml.Replace("<p><br></p>", "<br>");
				//fullJob.DescriptionHtml.Replace(@"\", "");
				//fullJob.DescriptionHtml.Replace("\"\\n", "");
				//fullJob.DescriptionHtml.Replace("\\n\"", "");
				//fullJob.DescriptionHtml.Replace("\\n \"", "");
				/**/
				fullJob.ForeignJobId = document.QuerySelector(".u-select-all").TextContent;
				fullJob.NumberOfPositions = job.NumberOfPositions;
				/**/
				var website = document.QuerySelector(".img-format--ratio16by9 > a");
				if (website != null) {
					fullJob.website = website.GetAttribute("href");
				}
				/**/
				ExtractJobTags(fullJob, document);
				/**/
				ExtractDefinitionLists(fullJob, document);
				fullJobList.Add(fullJob);
			}
			return fullJobList;
		}

		private static void ExtractJobTags(DescriptiveJobModel fullJob, IDocument document) {
			var tags = document.Body.SelectSingleNode("/html/body/main/div/div[3]/div[1]/div/section[5]/p");
			if (tags != null) {
				char[] spearator = { ',' };
				String[] strlist = tags.TextContent.Split(spearator);
				foreach (string tag in strlist) {
					fullJob.Tags.Add(tag);
				}
			}
		}

		private static void ExtractDefinitionLists(DescriptiveJobModel fullJob, IDocument document) {
			var jobInfo = document.QuerySelectorAll(".definition-list");

			string previousHead = "";
			foreach (var section in jobInfo) {
				foreach (var des in section.Children) {
					if (des.ToHtml().ToLower().StartsWith("<dt>")) {
						previousHead = des.TextContent.ToLower();
					} else {
						switch (previousHead) {
							case "arbeidsgiver":
							fullJob.Admissioner = des.TextContent;
							break;

							case "stillingstittel":
							fullJob.PositionTitle = des.TextContent;
							break;

							case "frist":
							fullJob.Deadline = des.TextContent;
							break;

							case "ansettelsesform":
							fullJob.PositionType = des.TextContent;
							break;

							case "bransje":
							fullJob.Industry = des.TextContent;
							break;

							case "stillingsfunksjon":
							//Category
							//ToDo
							//Split tags '/'
							fullJob.Category.Add(des.TextContent);
							break;

							case "sted":
							fullJob.LocationAdress = des.TextContent;
							break;

							case "sektor":
							fullJob.section = des.TextContent;
							break;

							case "kontaktperson":
							fullJob.AdmissionerContactPerson = des.TextContent;
							break;

							case "mobil":
							case "telefon":
							fullJob.AdmissionerContactPersonTelephone = des.TextContent.Replace("\n", "");
							break;

							//default:
							//Console.WriteLine($"@previous Head: '{previousHead}' - '{des.TextContent}'");
							//break;
						}
					}
				}
			}
		}

		private static void PrintInfo(List<JobModel> adverts) {
			int stillinger = 0;
			foreach (var jobb in adverts) {
				Console.WriteLine("\n");
				Console.WriteLine($"Id: {jobb.AdvertId}");
				Console.WriteLine($"tittel: {jobb.Position}");
				Console.WriteLine($"ShortDescription: {jobb.ShortDescription}");
				Console.WriteLine($"bilde: {jobb.ImageUrl}");
				Console.WriteLine($"url: {jobb.AdvertUrl}");
				stillinger++;
			}
			Console.WriteLine($"\nStillinger: {stillinger}");
			// TODO: Diff the data
		}

		[HttpGet]
		public async Task<string> GetAsync() {
			var result = await CheckForUpdates(websiteUrl, "Web-Scraper updates");

			return JsonConvert.SerializeObject(result);
		}
	}
}