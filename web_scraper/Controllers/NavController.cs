using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using web_scraper.Interfaces;
using web_scraper.models;

namespace web_scraper.Controllers {

	[Route("nav")]
	[ApiController]
	public class NavController : ControllerBase {
		private readonly INavApiRequest navApiRequest;

		public NavController(INavApiRequest navApiRequest) {
			this.navApiRequest = navApiRequest;
		}

		[HttpGet]
		public async Task<string> GetAsync() {
			List<JobModel> jobList = await navApiRequest.SendApiRequest();

			return JsonConvert.SerializeObject(jobList);
		}
	}
}