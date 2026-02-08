using UnityEngine;
using US13.Player;

namespace US13.Systems.Faith
{
	public interface IFaithProperty
	{
		public string FaithPropertyName { get; protected set; }
		public string FaithPropertyDesc { get; protected set; }
		public Sprite PropertyIcon { get; protected set; }
		public FaithData AssociatedFaith { get; set; }
		public void Setup(FaithData data);
		public void OnJoinFaith(PlayerScript newMember);
		public void OnLeaveFaith(PlayerScript member);
		public void RandomEvent();
	}
}