namespace Systems.CraftingV2
{
	public class CraftingActionParameters
	{
		private FeedbackType feedback;

		public FeedbackType Feedback => feedback;

		private bool ignoreToolsAndIngredients;

		/// <summary>
		/// 	Should we ignore tools and ingredients while checking?
		/// </summary>
		public bool IgnoreToolsAndIngredients => ignoreToolsAndIngredients;
		public static readonly CraftingActionParameters DefaultParameters
			= new CraftingActionParameters(false, FeedbackType.GiveAllFeedback);
		
		public static readonly CraftingActionParameters QuietParameters
			= new CraftingActionParameters(false, FeedbackType.GiveOnlySuccess);

		public CraftingActionParameters(bool ignoreToolsAndIngredients, FeedbackType feedback)
		{
			this.ignoreToolsAndIngredients = ignoreToolsAndIngredients;
			this.feedback = feedback;
		}
	}

	public enum FeedbackType
	{
		GiveAllFeedback,
		GiveNoFeedback,
		GiveOnlySuccess
	}
}