using System.Collections.Generic;
using HealthV2;
using Items.Weapons;
using UnityEngine;

namespace Systems.Faith.FaithProperties
{
	public class Transhumanist : IFaithProperty
	{
		[SerializeField] private Occupation chaplainOccupation;
		[SerializeField] private string chaplainBecomeBorgText;
		[SerializeField] private List<BodyPart> bodyPartsToMakeTranshumanist = new List<BodyPart>();
		[SerializeField] private Grenade pettyLeave;

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
			if (DMMath.Prob(50))
			{
				var EMP = Spawn.ServerPrefab(pettyLeave.gameObject);
				EMP.GameObject.GetComponent<Grenade>()?.Explode();
			}
		}

		public bool HasTriggeredFaithAction(PlayerScript memberWhoTriggered)
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