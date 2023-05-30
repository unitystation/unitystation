using UnityEngine;
using Mirror;

namespace Items.Robotics
{
	/// <summary>
	/// The component used in the botassembly prefabs that keeps track of the "stage" the bot is on before spawning the actual simplebot prefab
	/// </summary>
	public class BotConstruction : NetworkBehaviour, ICheckedInteractable<HandApply>
	{
		[Tooltip("Check this if the cyborg arm goes in last or if you need only one of the things in the list to be true")]
		public bool armLast;// A bool used if the bot should use the cyborg arm last

		[Tooltip("Place the parts used in each stage, the first part will be element 0")]
		public GameObject[] stageParts;// A list containing item prefabs set in the editor, the parts should go in the order you want

		[Tooltip("Place each sprite for each stage here, if the sprite should stay the same just leave it blank")]
		public Sprite[] stageSprite; // This list contains sprites for each stage, if left null sprite will not change

		[Tooltip("The bot that spawns when assembly is complete")]
		public GameObject botPrefab; // The simplebot prefab that will spawn when all stages are done

		[SyncVar(hook = nameof(SpriteSync))]
		private int stageCounter = 0; // A counter used to track what stage the bot is on and hooked to SpriteSync to sync the sprite with client

		public SpriteHandler spriteHandler;

		private void SpriteSync(int oldValue, int newValue)
		{
			// Syncs sprite with client
			if (stageSprite[stageCounter] != null)
			{
				spriteHandler.SetSprite(stageSprite[newValue]);
			}
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			// Gets the component ItemAttributesV2 and grabs the InitalName from the object in the hand
			string hand = interaction.HandObject != null ? interaction.HandObject.GetComponent<ItemAttributesV2>().InitialName : null;
			if (hand == null)
			{
				return false;
			}
			// Gets the component ItemAttributesV2 and grabs the InitalName from the object in the list according to the stageCounter
			string checkItem = stageParts[stageCounter].GetComponent<ItemAttributesV2>().InitialName;

			// Checks if armLast is true, if so it will accept anything in the list no matter the order
			if (armLast)
			{
				foreach (var part in stageParts)
				{
					if (hand == part.GetComponent<ItemAttributesV2>().InitialName) return true;
				}
				return false;
			}

			// Goes through list of items and checks them against the stageParts list and stageCounter
			for (int x = 0; x <= stageParts.Length - 1; x++)
			{
				if (hand == checkItem && x == stageCounter)
				{
					return true;
				}
			}
			return false;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			// Despawns item in hand, might cause problems later if it's stackable
			_ = Inventory.ServerDespawn(interaction.HandObject);

			if (armLast)
			{
				Spawn.ServerPrefab(botPrefab, gameObject.RegisterTile().WorldPosition, transform.parent, count: 1);
				stageCounter = 0;
				_ = Despawn.ServerSingle(gameObject);
			}

			// Checks to see if the stagecounter is greater than or equal to the length of stageParts list, if not ups the counter by 1
			if (stageCounter >= stageParts.Length - 1)
			{
				// Will spawn the simplebot, set stageCounter to 0 (sometimes causes problems with many instances) and despawns the assembly
				Spawn.ServerPrefab(botPrefab, gameObject.RegisterTile().WorldPosition, transform.parent, count: 1);
				stageCounter = 0;
				_ = Despawn.ServerSingle(gameObject);
			}
			else
			{
				stageCounter++;
			}
		}
	}
}
