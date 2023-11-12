using UnityEngine;
using UnityEngine.Serialization;

namespace Systems.Faith.FaithProperties
{
	public class HighLife : IFaithProperty
	{
		[SerializeField] private Sprite propertyIcon;
		string IFaithProperty.FaithPropertyName { get; set; } = "High life";
		string IFaithProperty.FaithPropertyDesc { get; set; } = "This faith relies on the absence of mind, and drinking all your problems away. Smoke illegal substances and drink alcohol to do your part.";

		Sprite IFaithProperty.PropertyIcon
		{
			get => propertyIcon;
			set => propertyIcon = value;
		}

		public FaithData AssociatedFaith { get; set; }

		public void Setup(FaithData associatedFaith)
		{
			//Todo: add high checks.
			AssociatedFaith = associatedFaith;
		}

		public void OnJoinFaith(PlayerScript newMember)
		{
			/* intentionally left empty */
		}

		public void OnLeaveFaith(PlayerScript member)
		{
			//Todo: add sobriety check.
		}

		public void RandomEvent()
		{
			//Todo: add random events for highlife.
		}
	}
}