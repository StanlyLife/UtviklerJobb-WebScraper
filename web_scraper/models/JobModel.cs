using System;
using System.ComponentModel.DataAnnotations;

namespace website_scraper.Models {

	public class JobModel {

		// [Key] defines a field as primary key
		[Key]
		public int AdvertId { get; set; }

		public string AdvertUrl { get; set; }
		public string Type { get; set; }
		public string Position { get; set; }
		public string Place { get; set; }
		public string Description { get; set; }
		public string Requirements { get; set; }
		public string ImageUrl { get; set; }
	}
}