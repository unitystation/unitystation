namespace Systems.Faith
{
	public interface IFaithProperty
	{
		public string FaithPropertyName { get; protected set; }
		public string FaithPropertyDesc { get; protected set; }
		public void Setup();
		public void OnJoinFaith(PlayerScript newMember);
		public void OnLeaveFaith(PlayerScript member);
		public bool HasTriggeredFaithAction(PlayerScript memberWhoTriggered);
		public void RandomEvent();
	}
}