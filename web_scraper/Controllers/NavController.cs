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

namespace web_scraper.Controllers {

	[Route("nav")]
	[ApiController]
	public class NavController : ControllerBase {
		private static readonly HttpClient client = new HttpClient();

		//This key is public and is found at: https://github.com/navikt/pam-public-feed
		public string ApiKey = "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJzdWIiOiJwdWJsaWMudG9rZW4udjFAbmF2Lm5vIiwiYXVkIjoiZmVlZC1hcGktdjEiLCJpc3MiOiJuYXYubm8iLCJpYXQiOjE1NTc0NzM0MjJ9.jNGlLUF9HxoHo5JrQNMkweLj_91bgk97ZebLdfx3_UQ";

		public string urlTest = "https://arbeidsplassen.nav.no/public-feed/api/v1/ads?uuid=410e4269-2e14-4602-8e7c-a1fa6222b301";
		public string url = "https://arbeidsplassen.nav.no/public-feed/api/v1/ads?page=1&size=5";

		public NavController() {
		}

		[HttpGet]
		public async Task<string> GetAsync() {
			client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("bearer", ApiKey);
			client.DefaultRequestHeaders.Add("accept", "application/json");
			HttpResponseMessage response = await client.GetAsync(url);

			var byteArray = response.Content.ReadAsByteArrayAsync().Result;
			var result = Encoding.UTF8.GetString(byteArray, 0, byteArray.Length);

			return JsonConvert.SerializeObject(result);
		}
	}
}