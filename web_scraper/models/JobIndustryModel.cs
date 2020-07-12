using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace web_scraper.models {

	public class JobIndustryModel {

		[Key]
		public int IndustryId { get; set; }

		[ForeignKey("JobModel")]
		public string JobId { get; set; }

		public string Industry { get; set; }
	}
}