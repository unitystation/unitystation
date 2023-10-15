namespace UnitystationLauncher.ContentScanning
{
	public enum InheritMode : byte
	{
		// Allow if All is set, block otherwise
		Default,
		Allow,

		// Block even is All is set
		Block
	}
}

