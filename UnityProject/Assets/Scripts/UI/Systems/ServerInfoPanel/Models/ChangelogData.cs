// ReSharper disable InconsistentNaming

using System;
using System.Collections.Generic;

namespace UI.Systems.ServerInfoPanel.Models
{
	[Serializable]
	public class Change
	{
		public string author_username { get; set; }
		public string author_url { get; set; }
		public string description { get; set; }
		public string pr_url { get; set; }
		public int pr_number { get; set; }
		public string category { get; set; }
		public string build { get; set; }
		public string date_added { get; set; }
	}

	[Serializable]
	public class Build
	{
		public string version_number { get; set; }
		public string date_created { get; set; }
		public List<Change> changes { get; set; }
	}

	[Serializable]
	public class AllChangesResponse
	{
		public int count { get; set; }
		public string next { get; set; }
		public string previous { get; set; }
		public List<Build> results { get; set; }
	}
}