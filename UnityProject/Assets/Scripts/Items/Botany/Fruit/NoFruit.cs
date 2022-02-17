using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Items.Botany.Fruit
{
	public class NoFruit : NetworkBehaviour, ICheckedInteractable<HandApply>
	{

		[SyncVar] private bool isReadyToBeHit = false;
		private SpriteHandler handler;

		[SerializeField] private List<GameObject> chanceToSpawn = new List<GameObject>();
		[SerializeField] private GameObject facehugger;
		//Since we can't get the sprites of prefabs before spawning them, we'll have to use a pre-made list that is made from the inspector
		[SerializeField] private List<SpriteDataSO> spritesToShow = new List<SpriteDataSO>();


		private void Awake()
		{
			handler = GetComponentInChildren<SpriteHandler>();
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			if (isReadyToBeHit == false && interaction.IsAltClick) return false;
			if (isReadyToBeHit && interaction.Intent != Intent.Harm) return false;
			return true;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (isReadyToBeHit == false)
			{
				isReadyToBeHit = true;
				Chat.AddExamineMsg(interaction.Performer, "You pull the tip of the nofruit and watch as multiple colors flash in front of you!");
				StartCoroutine(CycleSprites());
				return;
			}

			if (interaction.UsedObject == null || interaction.UsedObject.Item().Size < ItemSize.Medium)
			{
				var hasObject = interaction.UsedObject ? "a more" : "a";
				Chat.AddExamineMsg(interaction.Performer,
					$"You need {hasObject} blunt object to hit this!");
				return;
			}
			StopCoroutine(CycleSprites());
			if (DMMath.Prob(1f))
			{
				Spawn.ServerPrefab(facehugger, gameObject.AssumedWorldPosServer());
				return;
			}
			else
			{
				Spawn.ServerPrefab(chanceToSpawn[Random.Range(0, spritesToShow.Count - 1)], gameObject.AssumedWorldPosServer());
			}
			_ = Despawn.ServerSingle(gameObject);
		}

		private IEnumerator CycleSprites()
		{
			while (isReadyToBeHit || this != null)
			{
				handler.SetSpriteSO(spritesToShow[Random.Range(0, spritesToShow.Count - 1)]);
				yield return WaitFor.Seconds(0.3f);
			}
		}
	}
}