using System.Linq;
using UnityEngine;

namespace Systems.Faith.FaithProperties
{
	public class Xenophobia : IFaithProperty
	{
		[SerializeField] private Sprite propertyIcon;

		string IFaithProperty.FaithPropertyName { get; set; } = "Xenophobia";
		string IFaithProperty.FaithPropertyDesc { get; set; } = "Only the leaders' species of this faith is considered the 'acceptable' one.";

		Sprite IFaithProperty.PropertyIcon
		{
			get => propertyIcon;
			set => propertyIcon = value;
		}
		FaithData IFaithProperty.AssociatedFaith { get; set; }
		private FaithData Faith => ((IFaithProperty)this).AssociatedFaith;

		[SerializeField] private int nonMemberTakePoints = 10;
		[SerializeField] private int memberGivePoints = 15;

		public void Setup(FaithData associatedFaith)
		{
			((IFaithProperty)this).AssociatedFaith = associatedFaith;
		}

		public void OnJoinFaith(PlayerScript newMember)
		{
			//(Max): Need ideas
		}

		public void OnLeaveFaith(PlayerScript member)
		{
			//(Max): Need ideas
		}

		public void RandomEvent()
		{
			//TODO: Add Xenophobia events
		}
	}
}