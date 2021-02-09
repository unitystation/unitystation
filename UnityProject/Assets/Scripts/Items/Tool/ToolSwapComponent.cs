using System;
using System.Collections;
using System.Collections.Generic;
using AddressableReferences;
using UnityEngine;
using Mirror;

namespace Items
{
	/// <summary>
	/// Allows an item to change traits when it is activated. For example, the Jaws of Life.
	/// </summary>
	public class ToolSwapComponent : NetworkBehaviour, IExaminable, IInteractable<HandActivate>
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

		[SyncVar(hook = nameof(SyncState))]
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
			if (currentStateIndex + 1 < states.Count)
			{
				currentStateIndex++;
			}
			else
			{
				currentStateIndex = 0;
			}

			spriteHandler.ChangeSprite(CurrentState.spriteIndex);
			SoundManager.PlayNetworkedAtPos(CurrentState.changeSound, interaction.PerformerPlayerScript.WorldPos);
			Chat.AddExamineMsgFromServer(interaction.Performer, CurrentState.changeMessage);
		}

		private void SyncState(int oldStateIndex, int newStateIndex)
		{
			currentStateIndex = newStateIndex;
			SetTraitState(oldStateIndex, newStateIndex);
		}

		private void SetTraitState(int oldStateIndex, int newStateIndex)
		{
			// Remove the current traits as part of the old state.
			foreach (ItemTrait trait in states[oldStateIndex].traits)
			{
				itemAttributes.RemoveTrait(trait);
			}

			// Add the new traits from the new state.
			foreach (ItemTrait trait in states[newStateIndex].traits)
			{
				itemAttributes.AddTrait(trait);
			}
		}

		[Serializable]
		public struct ToolState
		{
			public string ExamineMessage;
			public string changeMessage;
			public AddressableAudioSource changeSound;
			public int spriteIndex;
			public ItemTrait[] traits;
			public AddressableAudioSource usingSound;
		}
	}
}
