using System;
using System.Collections.Generic;
using HealthV2;
using InGameEvents;
using Items.Weapons;
using Objects.Engineering;
using UnityEngine;
using UnityEngine.Serialization;

namespace Systems.Faith.FaithProperties
{
	public class Transhumanist : IFaithProperty
	{
		[SerializeField] private Occupation chaplainOccupation;
		[SerializeField] private string chaplainBecomeBorgText;
		[SerializeField] private List<BodyPart> bodyPartsToMakeTranshumanist = new List<BodyPart>();
		[SerializeField] private Grenade pettyLeave;

		[SerializeField] private string faithPropertyName = "Transhumanist";
		[SerializeField] private string faithPropertyDesc = "This faith believes the certainty of steel, and worship is unlocked via body modifications that unlocks one's true potential.";
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
			//Todo: Finish transhumanist setup to include checks for body status
		}

		public void OnJoinFaith(PlayerScript newMember)
		{
			HandleChaplain(newMember);
		}

		private void HandleChaplain(PlayerScript newMember)
		{
			if (newMember.Mind.occupation != chaplainOccupation) return;
			foreach (var parts in bodyPartsToMakeTranshumanist)
			{
				var newPart = Spawn.ClientPrefab(parts.gameObject);
				newMember.playerHealth.AddingBodyPart(newPart.GameObject.GetComponent<BodyPart>());
			}
			Chat.AddExamineMsg(newMember.GameObject, chaplainBecomeBorgText);
		}

		public void OnLeaveFaith(PlayerScript member)
		{
			if (DMMath.Prob(50) == false) return;
			var EMP = Spawn.ServerPrefab(pettyLeave.gameObject, member.gameObject.AssumedWorldPosServer());
			EMP.GameObject.GetComponent<Grenade>()?.Explode();
		}

		public void RandomEvent()
		{
			List<Action> randomEvents = new List<Action>()
			{
				EventKillerFish,
				EventBlessedGenerators
			};
			randomEvents.PickRandom().Invoke();
		}

		private void EventBlessedGenerators()
		{
			Chat.AddGameWideSystemMsgToChat("<color=#e6b800>The generators are blessed with fuel..</color>");
			var generators = MatrixManager.MainStationMatrix.GameObject.GetComponentsInChildren<PowerGenerator>();
			foreach (var generator in generators)
			{
				generator.SetFuel(generator.FuelAmount + 50f);
				generator.ToggleOn();
			}
		}

		private void EventKillerFish()
		{
			InGameEventsManager.Instance.TriggerSpecificEvent("Carp Migration", DMMath.Prob(50), DMMath.Prob(50));
		}
	}
}