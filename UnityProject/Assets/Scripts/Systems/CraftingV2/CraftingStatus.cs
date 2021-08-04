namespace Systems.CraftingV2
{
	public enum CraftingStatus
	{
		AllGood, // we can craft
		NotEnoughIngredients,
		NotEnoughReagents,
		NotEnoughTools,
		NotAbleToCraft,
		UnspecifiedImpossibility // something abnormal has happened
	}
}