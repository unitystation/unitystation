namespace UnitystationLauncher.ContentScanning
{
	public sealed class WhitelistFieldDefine
	{
		public string Name { get; }
		public MType FieldType { get; }

		public WhitelistFieldDefine(string name, MType fieldType)
		{
			Name = name;
			FieldType = fieldType;
		}
	}
}
