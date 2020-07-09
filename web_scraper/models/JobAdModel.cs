using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using web_scraper.models;

namespace website_scraper.Models {

	public class JobAdModel {

		// [Key] defines a field as primary key
		[Key]
		public string AdvertId { get; set; }

		/*Url*/
		public string AdvertUrl { get; set; }
		public string ImageUrl { get; set; }
		public string Position { get; set; }
		public string NumberOfPositions { get; set; }
		public string Admissioner { get; set; }
		public string ShortDescription { get; set; }
	}
}