using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using web_scraper.Data;
using web_scraper.Interfaces;
using web_scraper.Interfaces.Implementations;
using web_scraper.Interfaces.JobRetrievers;

namespace web_scraper {

	public class Startup {

		public Startup(IConfiguration configuration) {
			Configuration = configuration;
		}

		public IConfiguration Configuration { get; }

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services) {
			services.AddControllers();
			services.AddDbContext<WebScraperContext>(options => {
				var connectionString = "Data Source=(LocalDb)\\MSSQLLocalDB;" +
											   "database=LocalWebScraperDb;" +
											   "trusted_connection=yes;";

				var myMigrationAssembly = typeof(Startup).GetTypeInfo().Assembly.GetName().Name;
				options.UseSqlServer(connectionString, sql => {
					sql.MigrationsAssembly(myMigrationAssembly);
				});
			});
			services.AddTransient<INavApiRequest, NavApiRequest>();
			services.AddTransient<IFinnScraper, FinnScraper>();
			services.AddTransient<IKarrierestartScraper, KarrierestartScraper>();
			services.AddScoped<IJobHandler, JobHandler>();
			services.AddScoped<IJobTagHandler, JobTagHandler>();
			services.AddScoped<IJobCategoryHandler, JobCategoryHandler>();
			services.AddScoped<IJobIndustryHandler, JobIndustryHandler>();
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
			if (env.IsDevelopment()) {
				app.UseDeveloperExceptionPage();
			}

			app.UseHttpsRedirection();

			app.UseRouting();

			app.UseAuthorization();

			app.UseEndpoints(endpoints => {
				endpoints.MapControllers();
			});
		}
	}
}