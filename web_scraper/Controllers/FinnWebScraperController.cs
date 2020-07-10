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
using web_scraper.Interfaces;
using web_scraper.Interfaces.Implementations;

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
		private readonly IJobHandler jobHandler;
		private readonly IJobCategoryHandler jobCategoryHandler;
		private readonly IJobTagHandler jobTagHandler;

		public FinnWebScraperController(
			ILogger<FinnWebScraperController> logger,
			IJobHandler jobHandler,
			IJobCategoryHandler jobCategoryHandler,
			IJobTagHandler jobTagHandler
			) {
			_logger = logger;
			this.jobHandler = jobHandler;
			this.jobCategoryHandler = jobCategoryHandler;
			this.jobTagHandler = jobTagHandler;
		}

		private async Task<List<JobAdModel>> GetPageData(string url, List<JobAdModel> results) {
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
					JobAdModel advert = new JobAdModel();
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
					jobHandler.AddJobAd(advert);
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
			List<JobAdModel> adverts = new List<JobAdModel>();
			List<JobListingModel> advertsInfo = new List<JobListingModel>();

			/**
			 * GetPageData will recursively fill the container with data
			 * and the await keyword guarantees that nothing else is done
			 * before that operation is complete.
			 */
			adverts = await GetPageData(url, adverts);
			PrintInfo(adverts);

			advertsInfo = await FillDescriptiveJobModelAsync(adverts);

			jobHandler.SaveChanges();
			return new JobListModel {
				shortJobList = adverts,
				descriptiveJobList = advertsInfo
			};
		}

		private async Task<List<JobListingModel>> FillDescriptiveJobModelAsync(List<JobAdModel> jobs) {
			var config = Configuration.Default.WithDefaultLoader();
			var context = BrowsingContext.New(config);
			List<JobListingModel> fullJobList = new List<JobListingModel>();
			foreach (var job in jobs) {
				JobListingModel fullJob = new JobListingModel {
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
				await ExtractDefinitionListsAsync(fullJob, document);
				fullJobList.Add(fullJob);
			}
			return fullJobList;
		}

		private static void ExtractJobTags(JobListingModel fullJob, IDocument document) {
			//var tags = document.Body.SelectSingleNode("/html/body/main/div/div[3]/div[1]/div/section[5]/p");
			//if (tags != null) {
			//	char[] spearator = { ',' };
			//	String[] strlist = tags.TextContent.Split(spearator);
			//	foreach (string tag in strlist) {
			//		fullJob.Tags.Add(tag);
			//	}
			//}
		}

		private async Task ExtractDefinitionListsAsync(JobListingModel fullJob, IDocument document) {
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
							//REMOVE SPACES BEFORE AND AFTER CATEGORY
							await AddCategory(fullJob, des);
							jobCategoryHandler.SaveChanges();
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

		private async Task AddCategory(JobListingModel fullJob, IElement des) {
			char[] spearator = { '/' };
			String[] strlist = des.TextContent.Split(spearator);
			foreach (string category in strlist) {
				var categoryFormatted = category.Replace(",", "");
				JobCategoryModel tag = new JobCategoryModel {
					CategoryId = Guid.NewGuid().ToString(),
					AdvertId = fullJob.AdvertId,
					Category = categoryFormatted
				};
				if (!await jobCategoryHandler.JobIdHasCategory(fullJob.AdvertId, categoryFormatted)) {
					await jobCategoryHandler.AddJobCategory(tag);
					Console.WriteLine($"{fullJob.PositionTitle} - Added category: '{categoryFormatted}'");
				} else {
					Console.WriteLine($"{fullJob.PositionTitle} - duplicate category: '{categoryFormatted}'");
				}
			}
		}

		private static void PrintInfo(List<JobAdModel> adverts) {
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
			jobHandler.Purge();
			jobCategoryHandler.Purge();
			var result = await CheckForUpdates(websiteUrl, "Web-Scraper updates");
			return JsonConvert.SerializeObject(result);
		}
	}
}