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
			proxy.HttpProxy = "80.120.86.242:46771";
			proxy.SslProxy = "103.28.121.58:3128";
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