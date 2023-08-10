namespace Systems.Score
{
	public abstract class ScoreEntry
	{
		private string scoreName;
		private ScoreCategory category;
		private ScoreAlignment alignment;

		public int ScoreValue { get; set; }

		public string ScoreName
		{
			get => scoreName;
			set => scoreName = value;
		}
		public ScoreCategory Category
		{
			get => category;
			set => category = value;
		}
		public ScoreAlignment Alignment
		{
			get => alignment;
			set => alignment = value;
		}
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