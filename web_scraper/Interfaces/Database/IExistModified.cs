using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace web_scraper.Interfaces.Database {

	public interface IExistModified {

		public bool CheckIfExists(string foreignId);

		public bool CheckIfModified(string foreignId, string description);
	}
}