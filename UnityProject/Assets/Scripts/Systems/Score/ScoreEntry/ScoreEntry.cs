namespace Systems.Score
{
	public abstract class ScoreEntry
	{
		public string ScoreName;
		public ScoreCategory Category;
		public ScoreAlignment Alignment;
	}

	public enum ScoreCategory
	{
		StationScore, //Used for rating anything on the station at the end of the round.
		AntagScore, //Used for rating anything antags do at the end of the round.
		MiscScore //Flexible for tracking scores during the round, does not appear at the end of the round.
	}

	public enum ScoreAlignment
	{
		Unspecified,
		Good,
		Bad,
		Weird
	}
}