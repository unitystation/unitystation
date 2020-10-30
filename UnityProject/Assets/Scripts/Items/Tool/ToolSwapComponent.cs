using System;
using System.Collections;
using System.Collections.Generic;
using AddressableReferences;
using UnityEngine;

namespace Items
{
	/// <summary>
	/// Allows an item to change traits when it is activated. For example, the Jaws of Life.
	/// </summary>
	public class ToolSwapComponent : MonoBehaviour, IExaminable, IInteractable<HandActivate>
	{
		[Tooltip("The initial state the tool is in.")]
		[SerializeField]
		private int initialStateIndex = 0;

		[Tooltip("The tool states which this item will be able to represent via a HandActivate toggle in-game. " +
				"Effectively, you'll want this list to be at least 2 entries large.")]
		[SerializeField]
		private List<ToolState> states = default;

		private ItemAttributesV2 itemAttributes;
		private SpriteHandler spriteHandler;

		private int currentStateIndex = 0;
		public ToolState CurrentState => states[currentStateIndex];

		#region Lifecycle

		private void Awake()
		{
			itemAttributes = GetComponent<ItemAttributesV2>();
			spriteHandler = GetComponentInChildren<SpriteHandler>();
		}

		private void Start()
		{
			if (!CustomNetworkManager.IsServer) return;
			currentStateIndex = initialStateIndex;
		}

		#endregion Lifecycle

		public string Examine(Vector3 worldPos = default)
		{
			return CurrentState.ExamineMessage;
		}

		public void ServerPerformInteraction(HandActivate interaction)
		{
			foreach (ItemTrait trait in CurrentState.traits)
			{
				itemAttributes.RemoveTrait(trait);
			}

			// Cycle though the list, keeping in check the size of said list.
			currentStateIndex++;
			if (currentStateIndex >= states.Count)
			{
				currentStateIndex = 0;
			}

			foreach (ItemTrait trait in CurrentState.traits)
			{
				itemAttributes.AddTrait(trait);
			}

			spriteHandler.ChangeSprite(CurrentState.spriteIndex);
			// JESTE_R
			SoundManager.PlayNetworkedAtPos(CurrentState.ChangeSound, interaction.PerformerPlayerScript.WorldPos);
			Chat.AddExamineMsgFromServer(interaction.Performer, CurrentState.changeMessage);
		}

		[Serializable]
		public struct ToolState
		{
			public string ExamineMessage;
			public string changeMessage;
			public string changeSound;
			public AddressableAudioSource ChangeSound;
			public int spriteIndex;
			public ItemTrait[] traits;
			public string usingSound;
			public AddressableAudioSource UsingSound;
		}
	}
}
