using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using NaughtyAttributes;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Items.Botany.Fruit
{
	public class NoFruit : NetworkBehaviour, ICheckedInteractable<HandApply>
	{

		[Serializable]
		class ObjectToSpawn
		{
			public GameObject fruit;
			public SpriteDataSO fruitSprite;
		}

		[SyncVar] private bool isReadyToBeHit = false;
		private SpriteHandler handler;
		private int currentIndex = 0;

		[SerializeField] private List<ObjectToSpawn> chanceToSpawn = new List<ObjectToSpawn>();
		[SerializeField] private GameObject facehugger;


		private void Awake()
		{
			chanceToSpawn.Shuffle();
			handler = GetComponentInChildren<SpriteHandler>();
		}

		private void OnDisable()
		{
			StopCoroutine(CycleSprites());
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

			if (interaction.UsedObject == null || interaction.UsedObject.Item().Size < Size.Medium)
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
				Spawn.ServerPrefab(chanceToSpawn[currentIndex].fruit, gameObject.AssumedWorldPosServer());
			}
			_ = Despawn.ServerSingle(gameObject);
		}

		private IEnumerator CycleSprites()
		{
			while (isReadyToBeHit || this != null)
			{
				handler.SetSpriteSO(chanceToSpawn[currentIndex].fruitSprite);
				yield return WaitFor.Seconds(0.3f);
				currentIndex = (currentIndex + 1) % chanceToSpawn.Count;
			}
		}

		[Button("Get Fruit Sprites")]
		public void GetSpritesForNullFields()
		{
			foreach (var fruitInList in chanceToSpawn)
			{
				if(fruitInList.fruitSprite != null) continue;
				fruitInList.fruitSprite =
					fruitInList.fruit.GetComponentInChildren<SpriteHandler>().GetCurrentSpriteSO();
			}
		}
	}
}