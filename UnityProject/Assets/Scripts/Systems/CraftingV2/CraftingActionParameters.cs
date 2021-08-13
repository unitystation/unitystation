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

		public static readonly CraftingActionParameters DefaultParameters
			= new CraftingActionParameters(true, false);

		public CraftingActionParameters(bool shouldGiveFeedback, bool ignoreToolsAndIngredients)
		{
			this.shouldGiveFeedback = shouldGiveFeedback;
			this.ignoreToolsAndIngredients = ignoreToolsAndIngredients;
		}
	}
}