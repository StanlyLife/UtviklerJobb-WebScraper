using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.XPath;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using web_scraper.models;

namespace web_scraper.Interfaces.JobRetrievers {

	public class FinnScraper {
		private readonly string websiteUrl = "https://www.finn.no/job/fulltime/search.html?filters=&occupation=0.23&occupation=1.23.244&occupation=1.23.83&page=1&sort=1";
		private readonly IJobHandler jobHandler;
		private readonly IJobCategoryHandler jobCategoryHandler;
		private readonly IJobTagHandler jobTagHandler;
		private readonly IJobIndustryHandler jobIndustryHandler;

		public FinnScraper(
			IJobHandler jobHandler,
			IJobCategoryHandler jobCategoryHandler,
			IJobTagHandler jobTagHandler,
			IJobIndustryHandler jobIndustryHandler
			) {
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

			var advertrows = document.QuerySelectorAll("article");
			if (advertrows != null) {
				foreach (var row in advertrows) {
					JobModel job = new JobModel {
						JobId = Guid.NewGuid().ToString(),
						OriginWebsite = "Finn"
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
						//TODO
						//	Format to integer
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
			string NextPageUrlFormat = "https://www.finn.no/job/fulltime/search.html";
			string NextPageUrl = "";

			var nextPageLink = document.QuerySelector(".button--icon-right");
			if (nextPageLink != null) {
				NextPageUrl = NextPageUrlFormat + nextPageLink.GetAttribute("href");
				Console.WriteLine("\n neste side funnet!");
				Console.WriteLine(nextPageLink.GetAttribute("href") + "\n");
			} else {
				Console.WriteLine("\nFINISHED\n");
				NextPageUrlFormat = "";
			}

			// If next page link is present recursively call the function again with the new url
			if (!String.IsNullOrEmpty(NextPageUrl)) {
				Console.WriteLine("checking next page: " + NextPageUrlFormat);
				return await GetPosition(NextPageUrl, results);
			}
			return results;
		}

		private async Task<List<JobModel>> CheckForUpdates() {
			// We create the container for the data we want
			List<JobModel> jobList = new List<JobModel>();
			var url = websiteUrl;

			/**
			 * GetPageData will recursively fill the container with data
			 * and the await keyword guarantees that nothing else is done
			 * before that operation is complete.
			 */
			jobList = await GetPosition(url, jobList);

			await GetPositionListing(jobList);

			return jobList;
		}

		//
		//	- Seeds model with information retrieved from the individual position url
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
				fullJob.DescriptionHtml = document.QuerySelector(".import-decoration").ToHtml();
				fullJob.Description = document.QuerySelector(".import-decoration").TextContent;

				/*Get Finn KODE*/
				fullJob.ForeignJobId = document.QuerySelector(".u-select-all").TextContent;

				/*Get Admissioner WEBSITE*/
				await GetAdmissionerWebsite(context, fullJob, document);

				/*Error handling of PositionTitle*/
				CheckAndGetPositionTitle(fullJob, document);
				/*Get MODIFIED date*/
				fullJob.Modified = document.Body.SelectSingleNode("/html/body/main/div/div[4]/table/tbody/tr[2]/td").TextContent;
				/*Get position TAGS*/
				GetPositionTags(fullJob, document);
				/*Get information from lists*/
				await GetPositionInfo(fullJob, document);
				/*Add jobb to list for api requests*/
				/*update job in database*/
				jobList.Add(jobHandler.UpdateJob(fullJob));
			}

			return jobList;
		}

		//
		//	- If position title is a number, change position title.
		//	- This method catches faulty strings due to some ads not adding a position title
		//	  and the extracted position title on website matches the xpath of deadline.
		private static void CheckAndGetPositionTitle(JobModel fullJob, IDocument document) {
			var positionTitle = document.Body.SelectSingleNode("/html/body/main/div/div[3]/div[1]/div/section[2]/dl/dd[2]").TextContent;
			if (Regex.IsMatch(positionTitle, @"^\d")) {
				fullJob.PositionTitle = fullJob.PositionHeadline;
			} else {
				fullJob.PositionTitle = positionTitle;
			}
		}

		private static async Task GetAdmissionerWebsite(IBrowsingContext context, JobModel fullJob, IDocument document) {
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
				var AdmissionerWebsiteHref = innerDocument.QuerySelector(".u-pr16");
				if (AdmissionerWebsiteHref != null)
					fullJob.AdmissionerWebsite = AdmissionerWebsiteHref.GetAttribute("href");
			}
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
					Tag = tag.Trim()
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
							//TODO
							//	Format and split into cells
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
							/*Uncomment to check unscraped information*/
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
	}
}