using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace web_scraper.models {

	public class JobTagsModel {

		[Key]
		public string TagId { get; set; }

		[ForeignKey("JobModel")]
		public string AdvertId { get; set; }

		public string tag { get; set; }
	}
}