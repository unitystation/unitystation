using System.Collections;
using System;
using UnityEngine;
using Mirror;
using Systems.Botany;

namespace Items.Botany
{
	public class CottonBundle : MonoBehaviour, ICheckedInteractable<HandActivate>
	{
		[Tooltip("What you get when you use this in your hand.")]
		[SerializeField]
		private GameObject result = null;

		private int seedModifier;

		private float cottonPotency;

		private int finalAmount;

		private GrownFood grownFood;

		private void Awake()
		{
			///Getting GrownFood so I can snag the potency value.
			grownFood = GetComponent<GrownFood>();
			cottonPotency = grownFood.GetPlantData().Potency;
			///calculating how much cotton/durathread you should get.
			seedModifier = Mathf.RoundToInt(cottonPotency / 25f);
			finalAmount = seedModifier + 1;
		}

		public bool WillInteract(HandActivate interaction, NetworkSide side)
		{
			return DefaultWillInteract.Default(interaction, side);
		}

		public void ServerPerformInteraction(HandActivate interaction)
		{
			_ = Despawn.ServerSingle(gameObject);
			Spawn.ServerPrefab(result, interaction.Performer.AssumedWorldPosServer(), count: finalAmount);
			Chat.AddExamineMsgFromServer(interaction.Performer, "You pull some raw material out of the bundle!");
		}
	}
}
