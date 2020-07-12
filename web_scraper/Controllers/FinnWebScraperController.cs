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
		private readonly IJobIndustryHandler jobIndustryHandler;

		public FinnWebScraperController(
			ILogger<FinnWebScraperController> logger,
			IJobHandler jobHandler,
			IJobCategoryHandler jobCategoryHandler,
			IJobTagHandler jobTagHandler,
			IJobIndustryHandler jobIndustryHandler
			) {
			_logger = logger;
			this.jobHandler = jobHandler;
			this.jobCategoryHandler = jobCategoryHandler;
			this.jobTagHandler = jobTagHandler;
			this.jobIndustryHandler = jobIndustryHandler;
		}

		private async Task<List<JobModel>> GetPosition(string url, List<JobModel> results) {
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
					JobModel job = new JobModel {
						JobId = Guid.NewGuid().ToString(),
						OriginWebsite = "Finn.no"
					};

					var position = row.QuerySelector(".ads__unit__content__keys");
					var info = row.QuerySelector(".ads__unit__link");

					if (position != null) {
						//Executing this if statement together with the one above causes
						//NullPointerException
						if (string.IsNullOrWhiteSpace(position.TextContent)) {
							job.PositionHeadline = info.TextContent;
						} else {
							job.PositionHeadline = position.TextContent;
							job.ShortDescription = info.TextContent;
						}
						Console.WriteLine(job.PositionHeadline);
					} else {
						job.PositionHeadline = info.TextContent;
					}

					var imageUrl = row.QuerySelector(".img-format__img");
					job.ImageUrl = imageUrl.GetAttribute("src");
					job.AdvertUrl = "https://finn.no" + info.GetAttribute("href");

					var contentList = row.QuerySelectorAll(".ads__unit__content__list");
					try {
						job.Admissioner = contentList[0].TextContent;
						job.NumberOfPositions = contentList[1].TextContent;
					} catch (Exception e) {
						Console.WriteLine($"Error occured when scraping {job.AdvertUrl} --- {e}");
					}
					await jobHandler.AddJobListing(job);
					jobHandler.SaveChanges();
					results.Add(job);
				}
			}

			// Check if a next page link is present
			string NexPageUrlFormat = "https://www.finn.no/job/fulltime/search.html";

			var nextPageLink = document.QuerySelector(".button--icon-right");
			if (nextPageLink != null) {
				NexPageUrlFormat = NexPageUrlFormat + nextPageLink.GetAttribute("href");
				Console.WriteLine("\n neste side funnet!");
				Console.WriteLine(nextPageLink.GetAttribute("href") + "\n");
			} else {
				Console.WriteLine("\nFINISHED\n");
				NexPageUrlFormat = "";
			}

			// If next page link is present recursively call the function again with the new url
			if (!String.IsNullOrEmpty(NexPageUrlFormat)) {
				Console.WriteLine("checking next page: " + NexPageUrlFormat);
				//return await GetPageData(nextPageUrl, results);
			}
			return results;
		}

		private async Task<List<JobModel>> CheckForUpdates(string url, string mailTitle) {
			// We create the container for the data we want
			List<JobModel> jobList = new List<JobModel>();

			/**
			 * GetPageData will recursively fill the container with data
			 * and the await keyword guarantees that nothing else is done
			 * before that operation is complete.
			 */
			jobList = await GetPosition(url, jobList);

			await GetPositionListing(jobList);

			return jobList;
		}

		//GetPostionListing
		// Seeds model with information retrieved from the individual position url
		private async Task<List<JobModel>> GetPositionListing(List<JobModel> jobs) {
			var config = Configuration.Default.WithDefaultLoader();
			var context = BrowsingContext.New(config);

			List<JobModel> jobList = new List<JobModel>();
			foreach (var job in jobs) {
				JobModel fullJob = jobHandler.GetJobListingById(job.JobId);
				var document = await context.OpenAsync(job.AdvertUrl);
				//ToDo
				//Remove "<p><br /></p>"
				//Remove "<p>&nbsp;</p>"
				//Error catching
				fullJob.DescriptionHtml = document.QuerySelector(".import-decoration").ToHtml();
				fullJob.Description = document.QuerySelector(".import-decoration").TextContent;
				//fullJob.DescriptionHtml.Replace(@"\n", " ");
				//fullJob.DescriptionHtml.Replace("<p><br></p>", "<br>");
				//fullJob.DescriptionHtml.Replace(@"\", "");
				//fullJob.DescriptionHtml.Replace("\"\\n", "");
				//fullJob.DescriptionHtml.Replace("\\n\"", "");
				//fullJob.DescriptionHtml.Replace("\\n \"", "");
				/**/
				fullJob.ForeignJobId = document.QuerySelector(".u-select-all").TextContent;
				//ToDo
				//	Format to dateTime
				fullJob.NumberOfPositions = job.NumberOfPositions;
				/**/
				//ToDo
				//	Someone has to clean thi

				///html/body/main/div/div[3]/div[2]/div/div/div/div/a/@href
				///
				//

				var websiteNode = document.Body.SelectSingleNode("/html/body/main/div/div[3]/div[2]/div/div/div/div/a/@href");
				var websiteElement = document.QuerySelector(".img-format--ratio16by9 > a");
				var websiteQueryElement = document.QuerySelector(".u-b1");
				if (websiteNode != null && websiteNode.ToString() != "AngleSharp.Html.Dom.HtmlAnchorElement") {
					fullJob.AdmissionerWebsite = websiteNode.ToString();
				} else if (websiteElement != null) {
					fullJob.AdmissionerWebsite = websiteElement.GetAttribute("href");
				} else if (websiteQueryElement != null) {
					fullJob.AdmissionerWebsite = websiteQueryElement.GetAttribute("href");
				}

				if (fullJob.AdmissionerWebsite != null && fullJob.AdmissionerWebsite.StartsWith("https://www.finn.no/")) {
					var innerDocument = await context.OpenAsync(fullJob.AdmissionerWebsite);
					//var AdmissionerWebsiteHref = innerDocument.Body.SelectSingleNode("/html/body/div[2]/div/div/div/section[3]/p/a/@href");
					var AdmissionerWebsiteHref = innerDocument.QuerySelector(".u-pr16");
					if (AdmissionerWebsiteHref != null)
						fullJob.AdmissionerWebsite = AdmissionerWebsiteHref.GetAttribute("href");
					//fullJob.AdmissionerWebsite = AdmissionerWebsiteHref.ToString();
				}

				/**/
				var positionTitle = document.Body.SelectSingleNode("/html/body/main/div/div[3]/div[1]/div/section[2]/dl/dd[2]").TextContent;
				if (Regex.IsMatch(positionTitle, @"^\d")) {
					fullJob.PositionTitle = fullJob.PositionHeadline;
				} else {
					fullJob.PositionTitle = positionTitle;
				}
				fullJob.Modified = document.Body.SelectSingleNode("/html/body/main/div/div[4]/table/tbody/tr[2]/td").TextContent;
				/*tags*/
				GetPositionTags(fullJob, document);
				//Retrieves information
				await GetPositionInfo(fullJob, document);
				//Add to database and jobList
				jobList.Add(jobHandler.UpdateJob(fullJob));
			}

			return jobList;
		}

		private void GetPositionTags(JobModel fullJob, IDocument document) {
			Console.WriteLine($"Tags for: {fullJob.AdvertUrl}");
			var tags = document.Body.SelectSingleNode("/html/body/main/div/div[3]/div[1]/div/section[5]/p");
			if (tags == null) {
				tags = document.Body.SelectSingleNode("/html/body/main/div/div[3]/div[1]/div/section[6]/p");
				if (tags == null)
					return;
			}
			char[] spearator = { ',' };
			String[] strlist = tags.TextContent.Split(spearator);
			foreach (string tag in strlist) {
				JobTagsModel jobTag = new JobTagsModel {
					JobId = fullJob.JobId,
					tag = tag.Trim()
				};
				jobTagHandler.AddJobTag(jobTag);
				jobTagHandler.SaveChanges();
			}
		}

		private async Task GetPositionInfo(JobModel fullJob, IDocument document) {
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

							case "frist":
							fullJob.Deadline = des.TextContent;
							break;

							case "ansettelsesform":
							fullJob.PositionType = des.TextContent;
							break;

							case "bransje":
							await AddIndustry(fullJob, des);
							break;

							case "stillingsfunksjon":
							await AddCategory(fullJob, des);
							break;

							case "sted":
							//ToDo
							//Format and split into cells
							fullJob.LocationAdress = des.TextContent;
							break;

							case "sektor":
							fullJob.Sector = des.TextContent;
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

		private async Task AddIndustry(JobModel fullJob, IElement des) {
			var industry = des.TextContent.Replace(",", "").Trim();
			JobIndustryModel JobIndustry = new JobIndustryModel {
				JobId = fullJob.JobId,
				Industry = industry,
			};
			await jobIndustryHandler.AddJobIndustry(JobIndustry);
			jobIndustryHandler.SaveChanges();
		}

		private async Task AddCategory(JobModel job, IElement des) {
			char[] spearator = { '/' };
			String[] strlist = des.TextContent.Split(spearator);
			foreach (string category in strlist) {
				var categoryFormatted = category.Replace(",", "");
				JobCategoryModel tag = new JobCategoryModel {
					JobId = job.JobId,
					Category = categoryFormatted.Trim()
				};
				if (!await jobCategoryHandler.JobIdHasCategory(job.JobId, categoryFormatted)) {
					await jobCategoryHandler.AddJobCategory(tag);
					jobCategoryHandler.SaveChanges();
				}
			}
		}

		[HttpGet]
		public async Task<string> GetAsync() {
			jobHandler.Purge();
			jobTagHandler.Purge();
			jobIndustryHandler.Purge();
			jobCategoryHandler.Purge();
			var result = await CheckForUpdates(websiteUrl, "Web-Scraper updates");
			return JsonConvert.SerializeObject(result);
		}
	}
}