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

		public const string COMMON_SCORE_LABORPOINTS = "laborPoints";
		public const string COMMON_SCORE_SCIENCEPOINTS = "sciencePoints";
		public const string COMMON_SCORE_RANDOMEVENTSTRIGGERED = "randomEventsTriggered";
		public const string COMMON_SCORE_FOODMADE = "foodmade";
		public const string COMMON_SCORE_HOSTILENPCDEAD = "hostileNPCdead";
		public const string COMMON_SCORE_HEALING = "healing";
		public const string COMMON_SCORE_FOODEATEN = "foodeaten";
		public const string COMMON_SCORE_EXPLOSION = "explosions";
		public const string COMMON_SCORE_CLOWNABUSE = "clownBeaten";
		public const string COMMON_HUG_SCORE_ENTRY = "hugsGiven";
		public const string COMMON_TAIL_SCORE_ENTRY = "tailsPulled";
		public const string COMMON_DOOR_ELECTRIC_ENTRY = "electricDoors";
		public const string FILTH_ENTRY = "filth";
		public const int HUG_SCORE_VALUE = 2;
		public const int TAIL_SCORE_VALUE = -2;
		private const int DEAD_CREW_SCORE = -250;
		private const int HURT_CREW_MINIMUM_SCORE = 50;
		private const int CLEAN_STATION_SCORE = 1250;
		private const int DIRTY_STATION_SCORE = -350;
	}
}