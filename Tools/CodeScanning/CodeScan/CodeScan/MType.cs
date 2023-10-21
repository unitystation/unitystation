namespace UnitystationLauncher.ContentScanning
{
	public record MType
	{
		public virtual bool WhitelistEquals(MType other)
		{
			return false;
		}

		public virtual bool IsCoreTypeDefined()
		{
			return false;
		}

		/// <summary>
		/// Outputs this type in a format re-parseable for the sandbox config whitelist.
		/// </summary>
		public virtual string? WhitelistToString()
		{
			return ToString();
		}
	}
}