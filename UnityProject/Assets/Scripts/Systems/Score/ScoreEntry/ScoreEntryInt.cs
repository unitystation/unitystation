namespace Systems.Score
{
	public class ScoreEntryInt : ScoreEntry
	{
		private int score = 0;
		public int Score
		{
			get => score;
			set => score = value;
		}
	}
}