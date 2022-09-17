using UnityEngine;

namespace Systems.Score
{
	public partial class RoundEndScoreBuilder
	{
		/// <summary>
		/// How much does score entry that returns true or false score?
		/// </summary>
		[SerializeField] private int boolScore = 10;
		[SerializeField] private int negativeModifer = -5;
		[SerializeField] private Occupation captainOccupation;

		public static string COMMON_SCORE_LABORPOINTS = "laborPoints";
		public static string COMMON_SCORE_RANDOMEVENTSTRIGGERED = "randomEventsTriggered";
		public static string COMMON_SCORE_FOODMADE = "foodmade";
		public static string COMMON_SCORE_HOSTILENPCDEAD = "hostileNPCdead";
		public static string COMMON_SCORE_HEALING = "healing";
		public static string COMMON_SCORE_FOODEATEN = "foodeaten";
		public static string COMMON_SCORE_EXPLOSION = "explosions";
		public static string COMMON_SCORE_CLOWNABUSE = "clownBeaten";
		public static int DEAD_CREW_SCORE = -250;
	}
}