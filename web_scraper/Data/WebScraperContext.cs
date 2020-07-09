using Microsoft.EntityFrameworkCore;
using web_scraper.models;
using website_scraper.Models;

namespace web_scraper.Data {

	public class WebScraperContext : DbContext {
		public DbSet<JobAdModel> Jobs { get; set; }
		public DbSet<JobListingModel> JobListings { get; set; }
		public DbSet<JobTagsModel> JobTags { get; set; }
		public DbSet<JobCategoryModel> JobCategories { get; set; }

		public WebScraperContext(DbContextOptions<WebScraperContext> options) : base(options) {
			Database.EnsureCreated();
		}
	}
}