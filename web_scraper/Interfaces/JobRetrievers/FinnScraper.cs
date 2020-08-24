using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.XPath;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using web_scraper.Interfaces.Database;
using web_scraper.models;

namespace web_scraper.Interfaces.JobRetrievers {

	public class FinnScraper : IFinnScraper {
		private readonly string websiteUrl = "https://www.finn.no/job/fulltime/search.html?filters=&occupation=0.23&occupation=1.23.244&occupation=1.23.83&page=1&sort=1";
		private readonly IJobHandler jobHandler;
		private readonly IJobCategoryHandler jobCategoryHandler;
		private readonly IJobTagHandler jobTagHandler;
		private readonly IJobIndustryHandler jobIndustryHandler;
		private readonly IExistModified existModified;

		public FinnScraper(
			IJobHandler jobHandler,
			IJobCategoryHandler jobCategoryHandler,
			IJobTagHandler jobTagHandler,
			IJobIndustryHandler jobIndustryHandler,
			IExistModified existModified
			) {
			this.jobHandler = jobHandler;
			this.jobCategoryHandler = jobCategoryHandler;
			this.jobTagHandler = jobTagHandler;
			this.jobIndustryHandler = jobIndustryHandler;
			this.existModified = existModified;
		}

		/*
		 * TODO
		 * - Add support for categories (?)
		 * - Remove "<p><br /></p>"
		 */

		public async Task<List<JobModel>> CheckForUpdates() {
			var url = websiteUrl;
			List<JobModel> newJobs = new List<JobModel>();
			List<JobModel> existingJobs = new List<JobModel>();
			var config = Configuration.Default.WithDefaultLoader();
			var context = BrowsingContext.New(config);

			var jobLists = await GetJobAds(url, newJobs, existingJobs, context);

			newJobs = await GetPositionListing(jobLists[0], context);
			existingJobs = await CheckModifiedAndUpdate(jobLists[1], context);
			return newJobs;
		}

		private async Task<List<List<JobModel>>> GetJobAds(string url, List<JobModel> newJobs, List<JobModel> existingJobs, IBrowsingContext context) {
			var document = await context.OpenAsync(url);

			var advertrows = document.QuerySelectorAll("article");
			if (advertrows != null) {
				foreach (var row in advertrows) {
					JobModel job = new JobModel {
						JobId = Guid.NewGuid().ToString(),
						OriginWebsite = "Finn"
					};

					var info = row.QuerySelector(".ads__unit__link");
					job.AdvertUrl = "https://finn.no" + info.GetAttribute("href");
					job.ForeignJobId = info.GetAttribute("id");
					/*
					 * If job already exists in database
					 * Add to existingJobs list
					 * Check if modified
					 */
					if (existModified.CheckIfExists(job.ForeignJobId)) {
						existingJobs.Add(job);
						Console.WriteLine($"Job already exists code: {job.ForeignJobId}");
						continue;
					}

					var position = row.QuerySelector(".ads__unit__content__keys");

					if (position != null) {
						if (string.IsNullOrWhiteSpace(position.TextContent)) {
							job.PositionHeadline = info.TextContent;
						} else {
							job.PositionHeadline = position.TextContent;
							job.ShortDescription = info.TextContent;
						}
						Debug.WriteLine(job.PositionHeadline);
					} else {
						job.PositionHeadline = info.TextContent;
					}

					var imageUrl = row.QuerySelector(".img-format__img");
					job.ImageUrl = imageUrl.GetAttribute("src");

					var contentList = row.QuerySelectorAll(".ads__unit__content__list");
					try {
						job.Admissioner = contentList[0].TextContent;
						job.NumberOfPositions = contentList[1].TextContent;
					} catch (Exception e) {
						Console.WriteLine($"Error occured when scraping {job.AdvertUrl} --- {e}");
					}
					await jobHandler.AddJobListing(job);
					jobHandler.SaveChanges();
					newJobs.Add(job);
				}
			}

			// Check if a next page link is present
			string nextPageUrlFormat = "https://www.finn.no/job/fulltime/search.html";

			string NextPageUrl = GetNextPageUrl(document, nextPageUrlFormat);

			// If next page link is present recursively call the function again with the new url
			if (!String.IsNullOrEmpty(NextPageUrl)) {
				Console.WriteLine("checking next page: " + nextPageUrlFormat);
				await GetJobAds(NextPageUrl, newJobs, existingJobs, context);
			} else {
				Console.WriteLine($"nextpageurl is empty or null");
			}

			List<List<JobModel>> jobLists = new List<List<JobModel>>();
			jobLists.Add(newJobs);
			jobLists.Add(existingJobs);
			return jobLists;
		}

		private async Task<List<JobModel>> CheckModifiedAndUpdate(List<JobModel> existingJobs, IBrowsingContext context) {
			List<JobModel> modifiedJobs = new List<JobModel>();
			List<JobModel> unModifiedJobs = new List<JobModel>();

			foreach (var job in existingJobs) {
				var document = await context.OpenAsync(job.AdvertUrl);
				var descriptionHtml = document.QuerySelector(".import-decoration").ToHtml();

				//Check if Ad has expired
				//Apply button is always disabled when ad has expired
				if (document.QuerySelector(".button--is-disabled") != null) {
					Console.WriteLine($"Position has expired at: {job.AdvertUrl} with code: {job.ForeignJobId}");
				}

				if (existModified.CheckIfModified(job.ForeignJobId, descriptionHtml)) {
					modifiedJobs.Add(jobHandler.GetJobListingByForeignId(job.ForeignJobId));
				} else {
					unModifiedJobs.Add(job);
				}
			}
			modifiedJobs = await GetPositionListing(modifiedJobs, context);
			//Instead of creating a new list
			//I use existing list despite naming consistency
			modifiedJobs.AddRange(unModifiedJobs);
			return modifiedJobs;
		}

		private string GetNextPageUrl(IDocument document, string NextPageUrlFormat) {
			string nextPageUrl = "";
			var nextPageLink = document.QuerySelector(".button--icon-right");
			if (nextPageLink != null) {
				nextPageUrl = NextPageUrlFormat + nextPageLink.GetAttribute("href");
				Debug.WriteLine($"\n neste side funnet! \n {nextPageUrl} \n");
			} else {
				Debug.WriteLine("\nFINISHED\nNo next page cevron!");
				NextPageUrlFormat = "";
			}
			return nextPageUrl;
		}

		//	- Seeds model with information retrieved from the individual position url
		private async Task<List<JobModel>> GetPositionListing(List<JobModel> jobs, IBrowsingContext context) {
			List<JobModel> jobList = new List<JobModel>();
			foreach (var job in jobs) {
				JobModel fullJob = jobHandler.GetJobListingById(job.JobId);
				var document = await context.OpenAsync(job.AdvertUrl);
				fullJob.DescriptionHtml = document.QuerySelector(".import-decoration").ToHtml();
				fullJob.Description = document.QuerySelector(".import-decoration").TextContent;

				/*Get Admissioner WEBSITE*/
				await GetAdmissionerWebsite(context, fullJob, document);

				/*Error handling of PositionTitle*/
				CheckAndGetPositionTitle(fullJob, document);
				/*Get MODIFIED date*/
				fullJob.AdvertModified = document.Body.SelectSingleNode("/html/body/main/div/div[4]/table/tbody/tr[2]/td").TextContent;
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

		//
		//	- If position title is a number, change position title.
		//	- This method catches faulty strings due to some ads not adding a position title
		//	  and the extracted position title on website matches the xpath of deadline.
		private void CheckAndGetPositionTitle(JobModel fullJob, IDocument document) {
			var positionTitle = document.Body.SelectSingleNode("/html/body/main/div/div[3]/div[1]/div/section[2]/dl/dd[2]").TextContent;
			if (Regex.IsMatch(positionTitle, @"^\d")) {
				fullJob.PositionTitle = fullJob.PositionHeadline;
			} else {
				fullJob.PositionTitle = positionTitle;
			}
		}

		private async Task GetAdmissionerWebsite(IBrowsingContext context, JobModel fullJob, IDocument document) {
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