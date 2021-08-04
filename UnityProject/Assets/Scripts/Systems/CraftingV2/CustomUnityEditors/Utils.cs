namespace Systems.CraftingV2.CustomUnityEditors
{
	public static class Utils
	{
		// SampleText => sampleText
		public static string Title2Camel(string text)
		{
			return char.ToLowerInvariant(text[0]) + text.Substring(1);
		}
	}
}