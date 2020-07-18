using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace web_scraper.Services {

	public class SeleniumConfigService {

		public ChromeOptions SetProxyconfiguration(ChromeOptions chromeoptions) {
			Proxy proxy = new Proxy();
			proxy.Kind = ProxyKind.Manual;
			proxy.IsAutoDetect = false;
			proxy.HttpProxy = "84.211.221.252:1080";
			proxy.SslProxy = "84.211.221.252:1080";
			chromeoptions.Proxy = proxy;
			chromeoptions.AddArgument("ignore-certificate-errors");
			return chromeoptions;
		}

		public ChromeOptions SetDefaultChromeConfig(ChromeOptions chromeoptions) {
			chromeoptions.AddArguments(new List<string>() { "headless", "allow-running-insecure-content" });
			return chromeoptions;
		}
	}
}