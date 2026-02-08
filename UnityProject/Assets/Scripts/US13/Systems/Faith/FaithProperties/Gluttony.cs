using Logs;
using UnityEngine;
using US13.Core.Chat;
using US13.HealthV2.Living.MedicalChemistry;
using US13.HealthV2.Living.Metabolism;
using US13.HealthV2.Living.PolymorphicSystems;
using US13.Player;
using Util;

namespace US13.Systems.Faith.FaithProperties
{
	public class Gluttony : IFaithProperty
	{
		[SerializeField] private string faithPropertyName = "Gluttony";
		[SerializeField] private string faithPropertyDesc = "This faith believes that empty stomachs are a sign of weakness and a lack of a comfortable lifestyle.";

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

		public FaithData AssociatedFaith { get; set; }

		[SerializeField] private Sprite propertyIcon;

		public void Setup(FaithData associatedFaith)
		{
			FaithManager.Instance.FaithPropertiesEventUpdate.Add(CheckHungerLevels);
			AssociatedFaith = associatedFaith;
		}

		private void CheckHungerLevels()
		{
			foreach (var member in AssociatedFaith.FaithMembers)
			{
				if (member.playerHealth.TryGetSystem<HungerSystem>(out var hungerSystem) == false) return;
				switch (hungerSystem.CashedHungerState)
				{
					case HungerState.Full:
						Chat.AddExamineMsg(member.gameObject, "<color=green>My belly is full! I'm quite happy.</color>");
						FaithManager.AwardPoints(25, AssociatedFaith.Faith.FaithName);
						break;
					case HungerState.Normal:
						Chat.AddExamineMsg(member.gameObject, "I feel like I can grab a bite or two..");
						break;
					case HungerState.Hungry:
						Chat.AddExamineMsg(member.gameObject, "<i><color=yellow>I haven't ate anything in a while! I need to find something with high fat!</color></i>");
						FaithManager.TakePoints(10, AssociatedFaith.Faith.FaithName);
						break;
					case HungerState.Malnourished:
						Chat.AddExamineMsg(member.gameObject, "<i><color=yellow><size+=9>I must consume something! Anything!</size></color></i>");
						FaithManager.TakePoints(25, AssociatedFaith.Faith.FaithName);
						break;
					case HungerState.Starving:
						Chat.AddExamineMsg(member.gameObject, "<i><color=red><size+=12>I'M STARVING, THIS IS UNACCEPTABLE.</size></color></i>");
						FaithManager.TakePoints(45, AssociatedFaith.Faith.FaithName);
						StarvationProblem(member);
						break;
					default:
						Chat.AddExamineMsg(member.gameObject, "Food..");
						Loggy.Error("Unexpected case, did you add a new case and forget to update this code?");
						break;
				}
			}
		}

		private void StarvationProblem(PlayerScript member)
		{
			if (DMMath.Prob(25) == false) return;
			member.playerHealth.reagentPoolSystem.BloodPool.Add(CommonSicknesses.Instance.SpaceColdReagent, 1);
			//TODO: Change this from cold to custom sickness for this faith
		}

		public void OnJoinFaith(PlayerScript newMember)
		{
			if (newMember.playerHealth.TryGetSystem<HungerSystem>(out var hungerSystem) == false) return;
			hungerSystem.MakeStarving();
			Chat.AddExamineMsg(newMember.GameObject, "You suddenly have the urge to consume a lot of junk food and drink expensive beverages.");
		}

		public void OnLeaveFaith(PlayerScript member)
		{
			if (member.playerHealth.TryGetSystem<HungerSystem>(out var hungerSystem) == false) return;
			hungerSystem.MakeStarving();
		}

		public void RandomEvent()
		{
			//Todo: add random events for gluttony.
		}
	}
}