namespace US13.UI.Core.Net.Elements
{
	public class NetServerSyncString : NetUIStringElement
	{

		public override ElementMode InteractionMode => ElementMode.ServerWrite;

		private string CurrentString;

		public StringEvent OnChange;

		public override string Value
		{
			get
			{
				return CurrentString;
			}
			protected set
			{
				CurrentString = value;
				OnChange.Invoke(CurrentString);
			}
		}
	}
}
