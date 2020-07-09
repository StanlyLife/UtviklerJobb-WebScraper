using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using website_scraper.Models;

namespace web_scraper.models {

	public class JobListingModel {

		//REMOVE
		public JobListingModel() {
			//Category = new List<string>();
			//Tags = new List<string>();
		}

		[Key]
		public int JobListingId { get; set; }

		[ForeignKey("JobModel")]
		public string AdvertId { get; set; }

		/*Location*/
		public string LocationZipCode { get; set; }
		public string LocationCity { get; set; }
		public string LocationCounty { get; set; }
		public string LocationAdress { get; set; }

		/*Job primary info*/
		public string DescriptionHtml { get; set; }
		public string Description { get; set; }
		//public List<string> Category { get; set; }
		//public List<string> Tags { get; set; }
		/*Job secondary info*/
		public string NumberOfPositions { get; set; }
		public string Admissioner { get; set; }
		public string Deadline { get; set; }
		public string website { get; set; }
		public string AdmissionerContactPerson { get; set; }
		public string AdmissionerContactPersonTelephone { get; set; }
		public string PositionType { get; set; }
		public string PositionTitle { get; set; }
		public string Industry { get; set; }
		public string section { get; set; }
		/**/
		public string ForeignJobId { get; set; }
	}
}