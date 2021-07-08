using System.Collections.Generic;
using Managers;

namespace Strings
{
	public static class ChatTemplates
	{
		public static readonly string CaptainAnnounce =
			"\n\n<color=white><size=60><b>Captain Announces</b></size></color>\n\n"
			+ "<color=#FF151F><b>{0}</b></color>\n\n";

		public static readonly string CentcomAnnounce =
			"\n\n<color=white><size=60><b>Central Command Update</b></size></color>\n\n"
			+ "<color=#FF151F><b>{0}</b></color>\n\n";

		public static readonly string PriorityAnnouncement =
			"\n\n<color=white><size=60><b>Priority Announcement</b></size></color>\n\n"
			+ "<color=#FF151F>{0}</color>\n\n";

		public static readonly string ShuttleCallSub =
			"\n\nThe emergency shuttle has been called. It will arrive in {0} "
			+ "\nNature of emergency:" + "\n\n{1}";

		public static readonly string ShuttleRecallSub =
			"\n\nThe emergency shuttle has been recalled. \n\n{0}";


		public static readonly string CommandNewReport =
			"<color=#FF151F>Incoming Classified Message</color>\n\n"
			+ "A report has been downloaded and printed out at all communications consoles.";

		private static readonly string alertLevel =
			"<color=#FF151F><size=40><b>Attention! Security level {0}:</b></size></color>\n"+
			"<color=white><b>{1}</b></color>";

		public static string GetAlertLevelMessage(AlertLevelChange alertLevelChange)
		{
			return AlertLevelStrings[alertLevelChange];
		}


		private static readonly Dictionary<AlertLevelChange, string> AlertLevelStrings = new Dictionary<AlertLevelChange, string>
		{
			{
				AlertLevelChange.DownToGreen,

				string.Format(alertLevel,
					"lowered to green",
					"All threats to the station have passed. Security may not have weapons visible,"+
					" privacy laws are once again fully enforced.")
			},
			{
				AlertLevelChange.UpToBlue,

				string.Format(alertLevel,
					"elevated to blue",
					"The station has received reliable information about possible hostile activity"+
					" on the station. Security staff may have weapons visible, random searches are permitted.")

			},
			{
				AlertLevelChange.DownToBlue,

				string.Format(alertLevel,
					"lowered to blue",
					"The immediate threat has passed. Security may no longer have weapons drawn at all times,"+
					" but may continue to have them visible. Random searches are still allowed.")
			},
			{
				AlertLevelChange.UpToRed,

				string.Format(alertLevel,
					"elevated to red",
					"There is an immediate serious threat to the station. Security may have weapons unholstered"+
					" at all times. Random searches are allowed and advised.")
			},
			{
				AlertLevelChange.DownToRed,

				string.Format(alertLevel,
					"lowered to red",
					"The station's destruction has been averted. There is still however an immediate serious"+
					" threat to the station. Security may have weapons unholstered at all times, random searches"+
					" are allowed and advised.")
			},
			{
				AlertLevelChange.UpToDelta,

				string.Format(alertLevel,
					"elevated to delta",
					"Destruction of the station is imminent. All crew are instructed to obey all instructions"+
					" given by heads of staff. Any violations of these orders can be punished by death."+
					" This is not a drill.")
			}
		};
	}

	public enum AlertLevelChange
	{
		DownToGreen = CentComm.AlertLevel.Green,
		UpToBlue = CentComm.AlertLevel.Blue,
		DownToBlue = -CentComm.AlertLevel.Blue,
		UpToRed = CentComm.AlertLevel.Red,
		DownToRed = -CentComm.AlertLevel.Red,
		UpToDelta = CentComm.AlertLevel.Delta
	}
}