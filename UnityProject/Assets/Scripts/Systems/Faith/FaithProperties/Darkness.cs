using UnityEngine;

namespace Systems.Faith.FaithProperties
{
	public class Darkness : IFaithProperty
	{
		[SerializeField] private string faithPropertyName = "Darkness";
		[SerializeField] private string faithPropertyDesc = "People of this faith lurk in the darkness.";
		[SerializeField] private Sprite propertyIcon;

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

		Sprite IFaithProperty.PropertyIcon
		{
			get => propertyIcon;
			set => propertyIcon = value;
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