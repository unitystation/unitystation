using System;
using Logs;
using UnityEngine;
using Mirror;
using Util;

namespace Items.Robotics
{
	/// <summary>
	/// The component used in the bot assembly prefabs that keeps track of the "stage" the bot is on before spawning the actual simplebot mob
	/// </summary>
	public class BotConstruction : NetworkBehaviour, ICheckedInteractable<HandApply>
	{
		[Tooltip("Place the parts used in each stage, the first part will be element 0")]
		public GameObject[] stageParts;// A list containing item prefabs set in the editor, the parts should go in the order you want
		private string[] stagePartIDs; //The prefab ID of the assigned stage parts

		[Tooltip("Place each sprite for each stage here, if the sprite should stay the same just leave it blank")]
		public Sprite[] stageSprite; // This list contains sprites for each stage, if left null sprite will not change

		[Tooltip("The bot that spawns when assembly is complete")]
		public GameObject botBlueprint; // The simplebot prefab that will spawn when all stages are done

		[SyncVar(hook = nameof(SpriteSync))]
		private int stageCounter = 0; // A counter used to track what stage the bot is on and hooked to SpriteSync to sync the sprite with client

		public SpriteHandler spriteHandler;

		private void Awake()
		{
			stagePartIDs = new string[stageParts.Length];
			int i = 0;
			foreach (var part in stageParts)
			{
				if (part.TryGetComponent<PrefabTracker>(out var prefabTracker) == false)
				{
					Loggy.Error($"BotConstruction/Awake(): Could not find prefab tracker on construction step: {part.name}");
					stagePartIDs[i++] = "";
					continue;
				}

				stagePartIDs[i++] = prefabTracker.ForeverID;
			}
		}

		private void SpriteSync(int oldValue, int newValue)
		{
			// Syncs sprite with client
			if (stageSprite[stageCounter] != null)
			{
				spriteHandler.SetSpriteNonNetworked(stageSprite[newValue]);
			}
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			if(interaction.HandObject == false) return false;
			if (interaction.HandObject.TryGetComponent<PrefabTracker>(out var tracker))
			{
				if (stagePartIDs[stageCounter].Equals(tracker.ForeverID)) return true;
			}

			return false;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			// Despawns item in hand, might cause problems later if it's stackable
			Inventory.ServerConsume(interaction.HandSlot, 1);

			if (++stageCounter < stageParts.Length) return;

			// Will spawn the simple bot and despawn the assembly
			Spawn.ServerPrefab(botBlueprint, gameObject.RegisterTile().WorldPosition, transform.parent, count: 1);
			_ = Despawn.ServerSingle(gameObject);
		}
	}
}
