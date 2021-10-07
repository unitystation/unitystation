namespace Systems.CraftingV2
{
	public class CraftingActionParameters
	{
		private bool shouldGiveFeedback;

		/// <summary>
		/// 	Should we send feedback about a crafting status to the player?
		/// </summary>
		public bool ShouldGiveFeedback => shouldGiveFeedback;

		private bool ignoreToolsAndIngredients;

		/// <summary>
		/// 	Should we ignore tools and ingredients while checking?
		/// </summary>
		public bool IgnoreToolsAndIngredients => ignoreToolsAndIngredients;

		private bool shouldProclaimSuccess;

		/// <summary>
		/// 	Should we send feedback about a SUCCESSFUL crafting status to the player?
		/// </summary>
		public bool ShouldProclaimSuccess => shouldProclaimSuccess;

		public static readonly CraftingActionParameters DefaultParameters
			= new CraftingActionParameters(true, false, true);
		
		public static readonly CraftingActionParameters QuietParameters
			= new CraftingActionParameters(false, false, true);

		public CraftingActionParameters(bool shouldGiveFeedback, bool ignoreToolsAndIngredients, bool shouldProclaimSuccess)
		{
			this.shouldGiveFeedback = shouldGiveFeedback;
			this.ignoreToolsAndIngredients = ignoreToolsAndIngredients;
			this.shouldProclaimSuccess = shouldProclaimSuccess;
		}
	}
}