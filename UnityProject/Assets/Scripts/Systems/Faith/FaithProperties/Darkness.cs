using UnityEngine;

namespace Systems.Faith.FaithProperties
{
	public class Darkness : IFaithProperty
	{
		private string faithPropertyName;
		private string faithPropertyDesc;
		[SerializeField] private Sprite faithIcon;

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

		Sprite IFaithProperty.FaithIcon
		{
			get => faithIcon;
			set => faithIcon = value;
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

		public bool HasTriggeredFaithInaction(PlayerScript lazyMember)
		{
			throw new System.NotImplementedException();
		}

		public void Reward(PlayerScript member)
		{
			throw new System.NotImplementedException();
		}

		public void Sin(PlayerScript member)
		{
			throw new System.NotImplementedException();
		}

		public void RandomEvent()
		{
			throw new System.NotImplementedException();
		}
	}
}