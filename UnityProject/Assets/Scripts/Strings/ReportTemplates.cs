namespace Strings
{
	public static class ReportTemplates
	{
		public const string CENTCOM_REPORT = "<size=40><b>CentComm Report</b></size> " +
		                                     "\n __________________________________\n\n{0}";

		public const string INITIAL_UPDATE = "<color=white><size=40><b>{0}</b></size></color>\n\n"
		                                     + "<color=#FF151F>A summary has been copied and" +
		                                     " printed to all communications consoles</color>";

		public const string EXTENDED_INITIAL = "Thanks to the tireless efforts of our security and intelligence divisions, there are currently no credible threats to the station. All station construction projects have been authorized. Have a secure shift!";

		public const string BIO_HAZARD = "\n\n<color=#FF151F><size=60><b>BioHazard Report</b></size></color>\n\n"
		                                 + "<b>{0}</b>\n\n";

		public const string ANTAG_INITIAL_UPDATE = "Enemy communication intercepted. Security level elevated.";

		public const string ANTAG_THREAT_REPORT = "<size=26>Central Command has intercepted and partially decoded a Syndicate transmission with vital"+
		                                          " information regarding their movements.\n\n"+
		                                          "CentComm believes there might be Syndicate activity in the Station. We will keep you informed as we gather more "+
		                                          " intelligence.</size>\n\n"+
		                                          "<color=blue><size=32>Crew Objectives:</size></color>\n\n"+
		                                          "<size=24>- Subvert the threat.\n\n- Keep the productivity in the station.</size>";

		public const string STATION_OBJECTIVE = " <size=26>Asteroid bodies have been sighted in the local area around " +
		                                        "OutpostStation IV. Locate and exploit local sources for plasma deposits.</size>\n \n " +
		                                        "<color=blue><size=32>Crew Objectives:</size></color>\n \n <size=24>- Locate and mine " +
		                                        "local Plasma Deposits\n \n - Fulfill order of {0} Solid Plasma units and dispatch to " +
		                                        "Central Command via Cargo Shuttle</size>\n \n <size=32>Latest Asteroid Sightings:" +
		                                        "</size>\n \n";
	}
}