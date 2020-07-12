using Microsoft.EntityFrameworkCore;
using web_scraper.models;

namespace web_scraper.Data {

	public class WebScraperContext : DbContext {
		public DbSet<JobModel> JobListings { get; set; }
		public DbSet<JobTagsModel> JobTags { get; set; }
		public DbSet<JobCategoryModel> JobCategories { get; set; }
		public DbSet<JobIndustryModel> JobIndustry { get; set; }

		public WebScraperContext(DbContextOptions<WebScraperContext> options) : base(options) {
			Database.EnsureCreated();
		}
	}
}