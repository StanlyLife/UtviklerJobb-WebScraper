using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace web_scraper.models {

	public class JobTagsModel {

		[Key]
		public int TagId { get; set; }

		[ForeignKey("JobModel")]
		public string JobId { get; set; }

		public string tag { get; set; }
	}
}