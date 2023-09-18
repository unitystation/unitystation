namespace Systems.Faith.FaithProperties
{
	public class HighLife : IFaithProperty
	{
		private string faithPropertyName = "High life";
		private string faithPropertyDesc = "This faith relies on the absence of mind, and drinking all your problems away. Smoke illegal substances and drink alcohol to do your part.";

		string IFaithProperty.FaithPropertyName
		{
			get => faithPropertyName;
			set => faithPropertyName = value;
		}

		string IFaithProperty.FaithPropertyDesc
		{
			get => faithPropertyDesc;
			set => faithPropertyDesc = value;
		}

		public void Setup()
		{
		}

		public void OnJoinFaith(PlayerScript newMember)
		{
		}

		public void OnLeaveFaith(PlayerScript member)
		{
		}

		public void RandomEvent()
		{
		}
	}
}