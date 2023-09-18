namespace Systems.Faith.FaithProperties
{
	public class HighLife : IFaithProperty
	{
		private string faithPropertyName;
		private string faithPropertyDesc;

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
			throw new System.NotImplementedException();
		}

		public void OnJoinFaith(PlayerScript newMember)
		{
			throw new System.NotImplementedException();
		}

		public void OnLeaveFaith(PlayerScript member)
		{
			throw new System.NotImplementedException();
		}

		public void RandomEvent()
		{
			throw new System.NotImplementedException();
		}
	}
}