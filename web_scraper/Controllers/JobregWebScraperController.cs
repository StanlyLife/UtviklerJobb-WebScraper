using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Js;
using AngleSharp.XPath;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Razor.Language;
using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using web_scraper.models;

namespace web_scraper.Controllers {

	[ApiController]
	[Route("Jobreg")]
	public class JobregWebScraperController : ControllerBase {
		private readonly string websiteUrl = "https://www.jobreg.no/jobs.php?";
		private Stopwatch JobAdsTimer = new Stopwatch();
		private Stopwatch JobListingTimer = new Stopwatch();
		private readonly int GlobalMaxIteration = 15;
		private readonly int MaxPagePerQuery = 2;
		private int iteration = 0;
		private int currentPage = 1;

		/*
		 *
		 * Jobreg does not have categories publicly available.
		 * The only way to get categories without an API
		 * would be to manually visit every url parameter.
		 * Then match results to the category with lists or dictionaries.
		 *
		 */

		/*
		 *
		 * TODO
		 * - Add all categories
		 * - Add items to database
		 * - Check for duplicates system
		 * - Rework honeypot detector
		 * - refactor code
		 * - Add update database method: Check if already exist
		 *
		 */

		private Dictionary<string, string> BranchNames = new Dictionary<string, string>() {
			{ "&branchCategory[]=1",  "Bank / Finans / Forsikring" },
			{ "&branchCategory[]=2",  "Konsulenter / Frie yrker" },
			{ "&branchCategory[]=3",  "Forskning / Utvikling" },
			{ "&branchCategory[]=4",  "Helse / Sosial" },
			{ "&branchCategory[]=5",  "Hotell / Restaurant / Storhusholdning" },
			{ "&branchCategory[]=6",  "Håndverk / Bygg / Anlegg / Mekanikk" },
			{ "&branchCategory[]=7",  "Industri / Produksjon" },
			{ "&branchCategory[]=8",  "Interesseorganisasjoner" },
			{ "&branchCategory[]=9",  "IT / Telekommunikasjon / Internett" }, /*IT jobber*/
			{ "&branchCategory[]=10", "Jordbruk / Skogbruk / Jakt / Fiske" },
			{ "&branchCategory[]=11", "Kunst / Kultur" },
			{ "&branchCategory[]=12", "Media / Informasjon / Pr"},
			{ "&branchCategory[]=13", "Offentlige tjenester / forvaltning" },
			{ "&branchCategory[]=14", "Olje / Gass / Offshore / Onshore / Maritim" },
			{ "&branchCategory[]=15", "Personlige tjenester og servicebransjen" },
			{ "&branchCategory[]=16", "Renhold / Renovasjon" },
			{ "&branchCategory[]=17", "Salg / Markedsføring" },
			{ "&branchCategory[]=18", "Transport / Logistikk / Lager" },
			{ "&branchCategory[]=19", "Utdanning / Undervisning / forskning" },
			{ "&branchCategory[]=20", "Varehandel" },
			{ "&branchCategory[]=21", "Annet" },
			{ "&branchCategory[]=22", "Administrasjon / kontor / Personal" },
			{ "&branchCategory[]=23", "Økonomi / Regnskap" },
			{ "&branchCategory[]=24", "Ingeniøryrker" },
			{ "&branchCategory[]=25", "Reiseliv" },
			{ "&branchCategory[]=26", "Vann og rensing" },
			{ "&branchCategory[]=27", "Luftfart" },
		};

		private Dictionary<string, List<string>> BranchLinkCategories = new Dictionary<string, List<string>>() {
			{ "&branchCategory[]=1", new List<string>() {
				"&branch[]=1",
				"&branch[]=2",
				"&branch[]=3",
				"&branch[]=4",
				"&branch[]=5",
				"&branch[]=6",
				"&branch[]=7",
				"&branch[]=8",}
			},
			{ "&branchCategory[]=2", new List<string>() {
				"&branch[]=9",
				"&branch[]=10",
				"&branch[]=11",
				"&branch[]=12",
				"&branch[]=13",
				"&branch[]=14",
				"&branch[]=15",
				"&branch[]=16",
				"&branch[]=17",}
			},
			{ "&branchCategory[]=3", new List<string>() {
				"&branch[]=18",
				"&branch[]=19",
				"&branch[]=20",}
			},
			{ "&branchCategory[]=4", new List<string>() {
				"&branch[]=21",
				"&branch[]=22",
				"&branch[]=23",
				"&branch[]=24",
				"&branch[]=25",
				"&branch[]=26",
				"&branch[]=27",
				"&branch[]=28",
				"&branch[]=29",
				"&branch[]=30",}
			},
			{ "&branchCategory[]=5", new List<string>() {
				"&branch[]=31",
				"&branch[]=32",
				"&branch[]=33",
				"&branch[]=34",
				"&branch[]=35",
				"&branch[]=36",
				"&branch[]=37",
				"&branch[]=38",
				"&branch[]=39",
				"&branch[]=40",}
			},
			{ "&branchCategory[]=6", new List<string>() {
				"&branch[]=41",
				"&branch[]=42",
				"&branch[]=43",
				"&branch[]=44",
				"&branch[]=45",
				"&branch[]=46",
				"&branch[]=47",
				"&branch[]=48",}
			},
			{ "&branchCategory[]=7", new List<string>() {
				"&branch[]=49",
				"&branch[]=50",
				"&branch[]=51",
				"&branch[]=52",
				"&branch[]=53",
				"&branch[]=54",
				"&branch[]=55",
				"&branch[]=56",
				"&branch[]=57",
				"&branch[]=58",
				"&branch[]=59",
				"&branch[]=60",
				"&branch[]=61",
				"&branch[]=62",}
			},
			{ "&branchCategory[]=8", new List<string>() {
				"&branch[]=63",
				"&branch[]=64",
				"&branch[]=65",
				"&branch[]=66",
				"&branch[]=67",}
			},
			{ "&branchCategory[]=9", new List<string>() {
				"&branch[]=68",
				"&branch[]=69",
				"&branch[]=70",
				"&branch[]=71",
				"&branch[]=72",
				"&branch[]=73",
				"&branch[]=74",}
			},
			{ "&branchCategory[]=10", new List<string>() {
				"&branch[]=75",
				"&branch[]=76",
				"&branch[]=77",
				"&branch[]=78",
				"&branch[]=79",
				"&branch[]=80",
				"&branch[]=81",}
			},
			{ "&branchCategory[]=11", new List<string>() {
				"&branch[]=82",
				"&branch[]=83",
				"&branch[]=84",
				"&branch[]=85",
				"&branch[]=86",
				"&branch[]=87",
				"&branch[]=88",}
			},
			{ "&branchCategory[]=12", new List<string>() {
				"&branch[]=89",
				"&branch[]=90",
				"&branch[]=91",
				"&branch[]=92",
				"&branch[]=93",
				"&branch[]=94",
				"&branch[]=95",
				"&branch[]=96",}
			},
			{ "&branchCategory[]=13", new List<string>() {
				"&branch[]=97",
				"&branch[]=98",
				"&branch[]=99",
				"&branch[]=100",
				"&branch[]=101",
				"&branch[]=102",
				"&branch[]=103",
				"&branch[]=104",
				"&branch[]=105",
				"&branch[]=106",
				"&branch[]=107",
				"&branch[]=108",}
			},
			{ "&branchCategory[]=14", new List<string>() {
				"&branch[]=109",
				"&branch[]=110",
				"&branch[]=111",
				"&branch[]=112",
				"&branch[]=113",
				"&branch[]=114",
				"&branch[]=115",
				"&branch[]=116",
				"&branch[]=117",
				"&branch[]=118",
				"&branch[]=119",
				"&branch[]=120",
				"&branch[]=121",
				"&branch[]=122",}
			},
			{ "&branchCategory[]=15", new List<string>() {
				"&branch[]=123",
				"&branch[]=124",
				"&branch[]=125",
				"&branch[]=126",
				"&branch[]=127",
				"&branch[]=128",
				"&branch[]=129",
				"&branch[]=130",
				"&branch[]=131",}
			},
			{ "&branchCategory[]=16", new List<string>() {
				"&branch[]=132",
				"&branch[]=133",
				"&branch[]=134",
				"&branch[]=135",
				"&branch[]=136",}
			},
			{ "&branchCategory[]=17", new List<string>() {
				"&branch[]=137",
				"&branch[]=133",
				"&branch[]=134",
				"&branch[]=135",
				"&branch[]=143",}
			},
			{ "&branchCategory[]=18", new List<string>() {
				"&branch[]=144",
				"&branch[]=145",
				"&branch[]=146",
				"&branch[]=147",
				"&branch[]=148",
				"&branch[]=149",
				"&branch[]=150",
				"&branch[]=151",}
			},
			{ "&branchCategory[]=19", new List<string>() {
				"&branch[]=152",
				"&branch[]=153",
				"&branch[]=154",
				"&branch[]=155",
				"&branch[]=156",
				"&branch[]=157",
				"&branch[]=158",
				"&branch[]=159",
				"&branch[]=160",}
			},
			{ "&branchCategory[]=20", new List<string>() {
				"&branch[]=161",
				"&branch[]=162",
				"&branch[]=163",
				"&branch[]=164",
				"&branch[]=165",
				"&branch[]=166",
				"&branch[]=166",}
			},
			{ "&branchCategory[]=21", new List<string>() {
				"&branch[]=167",}
			},
			{ "&branchCategory[]=22", new List<string>() {
				"&branch[]=168",
				"&branch[]=169",
				"&branch[]=170",
				"&branch[]=171",
				"&branch[]=172",
				"&branch[]=173",
				"&branch[]=174",
				"&branch[]=175",
				"&branch[]=176",
				"&branch[]=177",}
			},
			{ "&branchCategory[]=23", new List<string>() {
				"&branch[]=178",
				"&branch[]=179",
				"&branch[]=180",
				"&branch[]=181",
				"&branch[]=182",
				"&branch[]=183",}
			},
			{ "&branchCategory[]=24", new List<string>() {
				"&branch[]=184",
				"&branch[]=185",
				"&branch[]=186",
				"&branch[]=187",
				"&branch[]=188",
				"&branch[]=189",
				"&branch[]=190",
				"&branch[]=191",
				"&branch[]=192",
				"&branch[]=193",
				"&branch[]=194",
				"&branch[]=195",
				"&branch[]=196",
				"&branch[]=197",
				"&branch[]=198",
				"&branch[]=199",}
			},
			{ "&branchCategory[]=25", new List<string>() {
				"&branch[]=200",
				"&branch[]=201",
				"&branch[]=202",
				"&branch[]=203",
				"&branch[]=204",
				"&branch[]=205",}
			},
			{ "&branchCategory[]=26", new List<string>() {
				"&branch[]=206",
				"&branch[]=207",
				"&branch[]=208",}
			},
		};

		private Dictionary<string, string> Categories = new Dictionary<string, string>() {
			/*Bank / Finans / forsikring*/
			{ "&branch[]=1", "Annet" },
			{ "&branch[]=2", "Rådgiver" },
			{ "&branch[]=3", "Saksbehandler" },
			{ "&branch[]=4", "Eiendomsmegling" },
			{ "&branch[]=5", "Megling / Analyse" }, /*Check*/
			{ "&branch[]=6", "Admisinistrativ Ledelse" },
			{ "&branch[]=7", "Bank / Finansielle tjenester" },
			{ "&branch[]=8", "Forskning / Fond / Pensjonsfond"},
			/*Konsulenter / Frieyrker*/
			{ "&branch[]=9", "Annet"},
			{ "&branch[]=10", "Rekruttering"},
			{ "&branch[]=11", "Arkitektvirksomhet"},
			{ "&branch[]=12", "Eiendomsforvaltning"},
			{ "&branch[]=13", "x"},
			{ "&branch[]=14", "Organsisasjonsutvikling"},
			{ "&branch[]=15", "x"},
			{ "&branch[]=16", "Forretningsutvikling / strategi"},
			{ "&branch[]=17", "Teknisk konsulentvirksomhet / Engineering"},
			/*Forskning / utvikling*/
			{ "&branch[]=18", "Annet"},
			{ "&branch[]=19", "x"},
			{ "&branch[]=20", "x"},
			/*Helse / sosial*/
			{ "&branch[]=21", "Annet"},
			{ "&branch[]=22", "x"},
			{ "&branch[]=23", "x"},
			{ "&branch[]=24", "Lege / Tannlege"},
			{ "&branch[]=25", "Sosialtjenester"},
			{ "&branch[]=26", "x"},
			{ "&branch[]=27", "x"},
			{ "&branch[]=28", "Apotektjenester / Farmasi"},
			{ "&branch[]=29", "Sykepleier og omsorgstjenester"},
			{ "&branch[]=30", "Psykiatri og psykologtjenester"},
			/*Hotell / Restaurant / Storhusholdning*/
			{ "&branch[]=31", "Annet"},
			{ "&branch[]=32", "x"},
			{ "&branch[]=33", "x"},
			{ "&branch[]=34", "x"},
			{ "&branch[]=35", "x"},
			{ "&branch[]=36", "x"},
			{ "&branch[]=37", "Kokk / Kjøkkenmedarbeider"},
			{ "&branch[]=38", "Restaurant og barvirkshomhet"},
			{ "&branch[]=39", "Kantine / Catering / Storhusholdning"},
			/*Håndverk / Bygg / anlegg / Mekanikk*/
			{ "&branch[]=40", "Annet"},
			{ "&branch[]=41", "Elektriker"},
			{ "&branch[]=42", "Bilmekaniker"},
			{ "&branch[]=43", "Bygg og anlegg"},
			{ "&branch[]=44", "x"},
			{ "&branch[]=45", "Servicemekaniker"},
			{ "&branch[]=46", "Tekniske byggfag"},
			{ "&branch[]=47", "Ufaglært håndverk"},
			{ "&branch[]=48", "Annet faglært håndværk"},
			/**/
			{ "&branch[]=49", "Annet"},
			{ "&branch[]=50", "Treindustri"},
			{ "&branch[]=51", "Verksindustri"},
			{ "&branch[]=52", "Grafisk industri"},
			{ "&branch[]=53", "Farmasøytisk industri"},
			{ "&branch[]=54", "Energi og vannforsyning"},
			{ "&branch[]=55", "Næring og nytelsesmidler"},
			{ "&branch[]=56", "Gummi, pløast og mineralvarer"},
			{ "&branch[]=57", "Tekstilindustri og bekledning"},
			{ "&branch[]=58", "Tre / Sagbruk / Tømmer"},
			{ "&branch[]=59", "Elektriske og optiske produkter"},
			{ "&branch[]=60", "Metallurgisk industri / Metallvarer"},
			{ "&branch[]=61", "Transportmidler / maskiner og utstyr"},
			{ "&branch[]=62", "Kjemisk industri / Petroleumsindustri"},
			/**/
			{ "&branch[]=63", "Annet"},
			{ "&branch[]=64", "Ideelle organisasjoner"},
			{ "&branch[]=65", "Arbeidstagerorgansiasjoner"},
			{ "&branch[]=66", "Ïnternasjonale organisasjoner"},
			{ "&branch[]=67", "Næringsliv og arbeidsgiverorganisasjon"},
			/**/
			{ "&branch[]=68", "Annet" },
			{ "&branch[]=69", "Support" },
			{ "&branch[]=70", "forretningsutvikling" },
			{ "&branch[]=71", "Applikasjonsutvikling" },
			{ "&branch[]=72", "Salg og markedsføring" },
			{ "&branch[]=73", "Drift av applikasjoner og infrastruktur" },
			{ "&branch[]=74", "Utvikling av infrastruktur og maskinvare" },
			/**/
			{ "&branch[]=75", "Annet" },
			{ "&branch[]=76", "x" },
			{ "&branch[]=77", "x" },
			{ "&branch[]=78", "Oppdrettnøring" },
			{ "&branch[]=79", "Fiske og fangst" },
			{ "&branch[]=80", "x" },
			{ "&branch[]=81", "Dyrking / Gartnerier" },
			/**/
			{ "&branch[]=82", "Annet" },
			{ "&branch[]=83", "x" },
			{ "&branch[]=84", "x" },
			{ "&branch[]=85", "Musikk / Dans / Drama" },
			{ "&branch[]=86", "x" },
			{ "&branch[]=87", "Bibliotek / Museer / Galleri" },
			{ "&branch[]=88", "x" },
			/**/
			{ "&branch[]=89", "Annet" },
			{ "&branch[]=90", "x" },
			{ "&branch[]=91", "Trykte media" },
			{ "&branch[]=92", "Journalistikk" },
			{ "&branch[]=93", "Annonse / Reklame" },
			{ "&branch[]=94", "x" },
			{ "&branch[]=95", "Kommunikasjonsrådgivning" },
			{ "&branch[]=96", "x" },
			/**/
			{ "&branch[]=97", "Annet" },
			{ "&branch[]=98", "x" },
			{ "&branch[]=99", "x" },
			{ "&branch[]=100", "Konsulent" },
			{ "&branch[]=101", "x" },
			{ "&branch[]=102", "x" },
			{ "&branch[]=103", "Helse / Sosial" },
			{ "&branch[]=104", "Tekniske tjenester" },
			{ "&branch[]=105", "Politi / Brann / Toll" },
			{ "&branch[]=106", "x" },
			{ "&branch[]=107", "Offentlige lederstillinger" },
			{ "&branch[]=108", "x" },
			/**/
			{ "&branch[]=109", "Annet" },
			{ "&branch[]=110", "x" },
			{ "&branch[]=111", "Engineering" },
			{ "&branch[]=112", "x" },
			{ "&branch[]=113", "Teknisk - maritim" },
			{ "&branch[]=114", "Olje og gassutvinning" },
			{ "&branch[]=115", "Operatør / Vedlikehold" },
			{ "&branch[]=116", "x" },
			{ "&branch[]=117", "x" },
			{ "&branch[]=118", "x" },
			{ "&branch[]=119", "x" },
			{ "&branch[]=120", "Drliing / Logging / Brønn / Boring" },
			{ "&branch[]=121", "x" },
			{ "&branch[]=122", "Tjenester tilknyttet olje og gassutvinning" },
			/**/
			{ "&branch[]=123", "x" },
			{ "&branch[]=124", "x" },
			{ "&branch[]=125", "x" },
			{ "&branch[]=126", "x" },
			{ "&branch[]=127", "x" },
			{ "&branch[]=128", "x" },
			{ "&branch[]=129", "x" },
			{ "&branch[]=130", "x" },
			{ "&branch[]=131", "x" },
			/**/
			{ "&branch[]=132", "Annet" },
			{ "&branch[]=133", "x" },
			{ "&branch[]=134", "x" },
			{ "&branch[]=135", "Vann, kloakk og renseanlegg" },
			{ "&branch[]=136", "x" },
			/**/
			{ "&branch[]=137", "Annet" },
			{ "&branch[]=138", "Kundesenter" },
			{ "&branch[]=139", "Produktsalg" },
			{ "&branch[]=140", "Teknisk salg" },
			{ "&branch[]=141", "Tjenestesalg" },
			{ "&branch[]=142", "Markedsføring" },
			{ "&branch[]=143", "Telemarketing" },
			/**/
			{ "&branch[]=144", "Annet" },
			{ "&branch[]=145", "x" },
			{ "&branch[]=146", "x" },
			{ "&branch[]=147", "Landtransport" },
			{ "&branch[]=148", "x" },
			{ "&branch[]=149", "Logistikk / Lager" },
			{ "&branch[]=150", "Post og budtjenester" },
			{ "&branch[]=151", "Transporttilknyttede tjenester" },
			/**/
			{ "&branch[]=152", "Annet" },
			{ "&branch[]=153", "Barnehage" },
			{ "&branch[]=154", "Grunnskole" },
			{ "&branch[]=155", "x" },
			{ "&branch[]=156", "x" },
			{ "&branch[]=157", "Videregående skole" },
			{ "&branch[]=158", "Universitet / Høgskole" },
			{ "&branch[]=159", "Voksenopplæring / kurs" },
			{ "&branch[]=160", "x" },
			/*Commodity Trade*/
			{ "&branch[]=161", "Annet" },
			{ "&branch[]=162", "x" },
			{ "&branch[]=163", "Franchisetager" },
			{ "&branch[]=164", "x" },
			{ "&branch[]=165", "Detaljhandel / butikksalg" },
			{ "&branch[]=166", "x" },
			/*Other*/
			{ "&branch[]=167", "Annet" },
			/*Administartion / OFfice /Personel*/
			{ "&branch[]=168", "Annet" },
			{ "&branch[]=169", "x" },
			{ "&branch[]=170", "x" },
			{ "&branch[]=171", "x" },
			{ "&branch[]=172", "x" },
			{ "&branch[]=173", "Arkiv / dokumentbehandling" },
			{ "&branch[]=174", "Kundeservice / Ordremottak" },
			{ "&branch[]=175", "Kontormedarbeider / Sekretær" },
			{ "&branch[]=176", "Personalarbeid / Rekruttering" },
			{ "&branch[]=177", "Administrativ ledelse / HR Manager" },
			/*Economy / Accounting*/
			{ "&branch[]=178", "Lønn" },
			{ "&branch[]=179", "Annet" },
			{ "&branch[]=180", "Revisjon" },
			{ "&branch[]=181", "Økonomi / Finans" },
			{ "&branch[]=182", "Innkjøp / Logistikk" },
			{ "&branch[]=183", "Regnskap / controlling" },
			/**/
			{ "&branch[]=184", "VVS" },
			{ "&branch[]=185", "x" },
			{ "&branch[]=186", "x" },
			{ "&branch[]=187", "Maskin" },
			{ "&branch[]=188", "Elektro" },
			{ "&branch[]=189", "Samferdsel" },
			{ "&branch[]=190", "x" },
			{ "&branch[]=191", "x" },
			{ "&branch[]=192", "Bygg og anlegg" },
			{ "&branch[]=193", "Energi / Kraft" },
			{ "&branch[]=194", "x" },
			{ "&branch[]=195", "Materialteknologi" },
			{ "&branch[]=196", "x" },
			{ "&branch[]=197", "x" },
			{ "&branch[]=198", "Automasjon / Instrumentering" },
			{ "&branch[]=199", "Geofag / Petroleumsteknologi" },
			/*Tourism*/
			{ "&branch[]=200", "Annet" },
			{ "&branch[]=201", "x" },
			{ "&branch[]=202", "x" },
			{ "&branch[]=203", "x" },
			{ "&branch[]=204", "x" },
			{ "&branch[]=205", "x" },
			/*Water and Sanitation*/
			{ "&branch[]=206", "Ingeniør" },
			{ "&branch[]=207", "x" },
			{ "&branch[]=208", "x" },
			/*Luftfart*/
		};

		public async Task<string> GetAsync() {
			JobAdsTimer.Start();
			var result = await CheckForUpdates(true);
			JobAdsTimer.Stop();
			Console.WriteLine($"@@@@@ Finished with {result.Count} results! @@@@@");
			return JsonConvert.SerializeObject(result);
		}

		private async Task<List<JobModel>> CheckForUpdates(bool checkCategories) {
			List<JobModel> jobList = new List<JobModel>();
			/**/
			var chromeoptions = new ChromeOptions();
			chromeoptions.AddArguments(new List<string>() { "headless", "allow-running-insecure-content" });
			var driver = new ChromeDriver(chromeoptions);
			/**/
			JobAdsTimer.Start();
			if (checkCategories) {
				foreach (var branch in BranchLinkCategories) {
					foreach (var category in branch.Value) {
						var categoryWebsiteUrl = websiteUrl + branch.Key + category;
						var categoryName = Categories.Where(x => x.Key.Equals(category)).Select(x => x.Value).FirstOrDefault();
						if (Categories.ContainsKey(category)) {
							Console.WriteLine($"\nCategory: {Categories.GetValueOrDefault(category)}");
						}
						/*Current category list*/
						var categoryQueryList = new List<string>();
						categoryQueryList.Add(branch.Key);
						categoryQueryList.Add(category);
						Console.WriteLine($"testing categortBranc {branch.Key} with category: {category}");
						/*execute scraper*/
						Console.WriteLine($"starting at {categoryWebsiteUrl}\n");
						currentPage = 1;
						List<JobModel> tempList = new List<JobModel>();
						tempList = await GetJobs(categoryWebsiteUrl, new List<JobModel>(), driver, categoryQueryList);
						Console.WriteLine($"@@@ Finished one category, found {tempList.Count()} jobs @@@");
						Console.WriteLine($"@@@ Finished one category, currently {jobList.Count()} jobs in joblist @@@");
						jobList.AddRange(tempList);
						Console.WriteLine($"@@@ Finished one category, added {tempList.Count()} jobs in joblist: {jobList.Count()} @@@");
					}
				}
			} else {
				jobList = await GetJobs(websiteUrl, new List<JobModel>(), driver, new List<string>());
			}
			JobAdsTimer.Stop();
			/**/
			JobListingTimer.Start();
			jobList = await GetListingInfoAsync(jobList, driver);
			JobListingTimer.Stop();
			/**/
			Console.WriteLine($"@@@@@ JobAdsTimer Finished after {JobAdsTimer.Elapsed} seconds! @@@@@");
			Console.WriteLine($"@@@@@ JobAdsTimer Finished after {JobListingTimer.Elapsed} seconds! @@@@@");
			/**/
			return jobList;
		}

		private async Task<List<JobModel>> GetListingInfoAsync(List<JobModel> jobs, ChromeDriver driver) {
			var config = Configuration.Default.WithDefaultLoader();
			var context = BrowsingContext.New(config);

			foreach (var job in jobs) {
				var document = await context.OpenAsync(job.AdvertUrl);
				Console.WriteLine($"\nURL: {job.AdvertUrl}");
				/*Admissioner*/
				GetListingAdmissionerInfo(job, document);
				/*GetTableContent*/
				GetListingTableContent(job, document);
			}
			//ToDo
			//	Add job to database
			return jobs;
		}

		private static void GetListingTableContent(JobModel job, IDocument document) {
			var tableInfoList = document.QuerySelectorAll("tr");
			foreach (var row in tableInfoList) {
				var title = row.QuerySelector(".table-info-title");
				var value = row.QuerySelector(".table-info-value");
				if (title != null && value != null) {
					switch (title.TextContent.ToLower()) {
						case "firma":
						job.Admissioner = value.TextContent;
						break;

						case "sted":
						job.LocationAdress = value.TextContent;
						break;

						case "by":
						job.LocationCity = value.TextContent;
						break;

						case "fylke":
						job.LocationCounty = value.TextContent;
						break;

						case "nettside":
						job.AdmissionerWebsite = value.GetAttribute("href");
						break;

						case "arbeidstittel":
						job.PositionTitle = value.TextContent;
						break;

						case "tiltredelse":
						job.Accession = value.TextContent;
						break;

						case "søknadsfrist":
						job.Deadline = value.TextContent;
						break;

						default:
						Console.WriteLine($"NULL {title.TextContent} -> {value.TextContent}");
						break;
					}
				} else {
					Console.WriteLine(row.ToHtml());
				}
			}
		}

		private static void GetListingAdmissionerInfo(JobModel job, IDocument document) {
			var contactPerson = document.Body.SelectSingleNode("/html/body/div[1]/header/div[2]/div/div/div/div/div[1]/div/div[2]/div/div[4]/div[2]/div[2]/span[1]/b");
			if (contactPerson != null) {
				job.AdmissionerContactPerson = contactPerson.TextContent;
			}

			var contactPersonTelepone = document.Body.SelectSingleNode("/html/body/div[1]/header/div[2]/div/div/div/div/div[1]/div/div[2]/div/div[4]/div[2]/div[2]/span[3]");
			if (contactPersonTelepone != null) {
				job.AdmissionerContactPersonTelephone = contactPersonTelepone.TextContent;
			}
			/*Description*/
			var desc = document.QuerySelector(".showBody");
			if (desc != null) {
				job.DescriptionHtml = desc.ToHtml();
				job.Description = desc.TextContent;
			} else {
				Console.WriteLine($"No jobdescription found for positon: {job.AdvertUrl}");
			}
			/*tags*/
			var tags = document.QuerySelectorAll(".label-default");
			if (tags != null) {
				foreach (var tag in tags) {
					JobTagsModel tagModel = new JobTagsModel() {
						JobId = job.JobId,
						Tag = tag.TextContent,
					};
					//ToDo
					//	Add tag to database
				}
			} else {
				Console.WriteLine($"No tags found for positon: {job.AdvertUrl}");
			}
		}

		private async Task<List<JobModel>> GetJobs(string url, List<JobModel> jobList, ChromeDriver driver, List<string> categoryQueryList) {
			if (iteration >= GlobalMaxIteration || currentPage >= MaxPagePerQuery) {
				Console.WriteLine("Max limit reached");
				return jobList;
			}
			iteration++;

			driver.Navigate().GoToUrl(url);
			WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(20));
			wait.Until(ExpectedConditions.ElementExists(By.CssSelector(".job-item")));
			IReadOnlyList<IWebElement> jobs = driver.FindElements(By.CssSelector(".job-item"));

			foreach (var jobItem in jobs) {
				JobModel job = new JobModel() {
					OriginWebsite = "jobreg",
					JobId = Guid.NewGuid().ToString(),
				};
				/**/
				//TODO
				// NULL ERROR HANDLING ON ADVERTURL
				IWebElement advertUrl;
				try {
					advertUrl = jobItem.FindElement(By.CssSelector(".adLogo > a"));
				} catch (NoSuchElementException) {
					advertUrl = jobItem.FindElement(By.CssSelector(".description > a"));
				}
				if (advertUrl != null) {
					/**/
					GetForeignJobId(job, advertUrl.GetAttribute("href"));
					/**/
					GetJobAdInfo(jobItem, job, advertUrl.GetAttribute("href"));
					/**/
					if (jobList.Contains(job)) {
						//Honeypot detection
						Console.WriteLine($"@@@@@@ \nPossible honeypot at {websiteUrl} \ncurrent page {currentPage} \nduplicate jobId {job.ForeignJobId} \njob url {job.AdvertUrl} \n@@@@@@");
						return jobList;
					}
					jobList.Add(job);
				}
			}
			Console.WriteLine($"Found {jobs.Count} jobs at page {currentPage}!");

			if (jobs.Count < 15) {
				return jobList;
			}
			/*
			 * CHECK NEXT PAGE
			 */
			string NextPageUrlFormat = "https://www.jobreg.no/jobs.php?";
			string NextPageUrlPagePrefix = "&start=";
			string NextPageUrlCategories = "";
			string NextPageUrl = "";
			string result = "";
			//IWebElement nextPageLink = driver.FindElement(By.XPath("/html/body/div/main/section/div/div/div/div/div[2]/div/ul/li[11]/a"));
			var nextPageLink = driver.FindElementsByCssSelector("#pagination li");
			if (nextPageLink != null /* && content == " > "*/) {
				foreach (var next in nextPageLink) {
					if (next.Text.Trim() == (currentPage + 1).ToString() || next.Text == " > " || next.Text.Trim() == ">") {
						result = GenerateNextPageUrl(next.FindElement(By.CssSelector("a")));
						Console.WriteLine("FOUND IT! " + next.Text);
						if (!string.IsNullOrEmpty(result)) {
							break;
						} else {
							Console.WriteLine($"Next page href is null!");
						}
					} else {
						Console.WriteLine($" #pagination li = '{next.Text}' --- next page == {currentPage + 1 }");
					}
				}
				if (!string.IsNullOrWhiteSpace(result)) {
					NextPageUrl = url + NextPageUrlCategories + NextPageUrlPagePrefix + result;
					Console.WriteLine($"\nNext page URL: '{NextPageUrl}' \n");
				} else {
					Console.WriteLine("did not find ' > '");
					Console.WriteLine($"result: {result}");
				}
			}

			// If next page link is present recursively call the function again with the new url
			if (!String.IsNullOrEmpty(NextPageUrl)) {
				Console.WriteLine("checking next page: " + NextPageUrl);
				currentPage++;
				return await GetJobs(NextPageUrl, jobList, driver, categoryQueryList);
			}

			return jobList;
		}

		private static string GenerateNextPageUrl(IWebElement nextPageLink) {
			var NextPageHref = nextPageLink.GetAttribute("href");
			NextPageHref = NextPageHref.Trim();
			int pFrom = NextPageHref.IndexOf("'") + "'".Length;
			int pTo = NextPageHref.LastIndexOf("'");
			string result = NextPageHref.Substring(pFrom, pTo - pFrom);
			result = result.Replace(" ", "%20");
			return result;
		}

		private static void GetJobAdInfo(IWebElement jobItem, JobModel job, string advertUrl) {
			var header = jobItem.FindElement(By.CssSelector("h6 > a")).Text;
			job.PositionHeadline = header;
			Console.WriteLine($"JOB: {header} \n");
			/**/
			job.AdvertUrl = advertUrl;
			/**/
			var shortDesc = jobItem.FindElement(By.CssSelector(".description > .adBodySmall")).Text;
			job.ShortDescription = shortDesc;
			/**/
			var positionAmount = jobItem.FindElement(By.CssSelector(".about > .positions")).Text;
			job.NumberOfPositions = positionAmount;
			/**/
			var positionType = jobItem.FindElement(By.CssSelector(".about > .type")).Text;
			job.PositionType = positionType;
			/**/
			try {
				var imageUrl = jobItem.FindElement(By.CssSelector(".adLogo img")).GetAttribute("src");
				job.ImageUrl = imageUrl;
			} catch (NoSuchElementException) {
				Console.WriteLine($"No image for job ad at : {job.AdvertUrl}");
			}
		}

		private static void GetForeignJobId(JobModel job, string advertUrl) {
			var IdFrom = advertUrl.LastIndexOf("-") + "-".Length;
			int IdTo = advertUrl.LastIndexOf(".html");
			var foreignJobId = advertUrl.Substring(IdFrom, IdTo - IdFrom);
			job.ForeignJobId = foreignJobId;
		}
	}
}