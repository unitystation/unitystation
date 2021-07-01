namespace Strings
{
	public static class ReportTemplates
	{
		public static readonly string CentcomReport = "<size=40><b>CentComm Report</b></size> " +
		                                              "\n __________________________________\n\n{0}";

		public static readonly string InitialUpdate = "<color=white><size=40><b>{0}</b></size></color>\n\n"
		                                              + "<color=#FF151F>A summary has been copied and" +
		                                              " printed to all communications consoles</color>";

		public static readonly string ExtendedInitial = "Thanks to the tireless efforts of our security and intelligence" +
		                                                " divisions, there are currently no credible threats to the station. " +
		                                                "All station construction projects have been authorized. Have a secure shift!";

		public static readonly string BioHazard = "\n\n<color=#FF151F><size=60><b>BioHazard Report</b></size></color>\n\n"
		                                          + "<b>{0}</b>\n\n";

		public static readonly string AntagInitialUpdate = "Enemy communication intercepted. Security level elevated.";

		public static readonly string AntagThreat =
			"<size=26>Central Command has intercepted and partially decoded a Syndicate transmission with vital"+
			" information regarding their movements.\n\n"+
			"CentComm believes there might be Syndicate activity in the Station. We will keep you informed as we gather more "+
			" intelligence.</size>\n\n"+
			"<color=blue><size=32>Crew Objectives:</size></color>\n\n"+
			"<size=24>- Subvert the threat.\n\n- Keep the productivity in the station.</size>";

	}
}