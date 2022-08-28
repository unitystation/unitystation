namespace Systems.Score
{
	public abstract class ScoreEntry
	{
		public string ScoreName;
		public ScoreCategory Category;
	}

	public enum ScoreCategory
	{
		StationScore,
		AntagScore,
		MiscScore
	}
}