namespace Systems.Score
{
	public class ScoreEntryBool : ScoreEntry
	{
		private bool score = false;

		public bool Score
		{
			get => score;
			set => score = value;
		}
	}
}