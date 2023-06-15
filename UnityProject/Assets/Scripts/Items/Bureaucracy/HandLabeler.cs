using Mirror;
using System.Collections;
using System.Collections.Generic;
using Objects;
using UnityEngine;

namespace Items
{
	public class HandLabeler : NetworkBehaviour, ICheckedInteractable<HandApply>, ICheckedInteractable<InventoryApply>, IClientInteractable<HandActivate>, IServerSpawn
	{
		public const int MAX_TEXT_LENGTH = 16;

		private const int LABEL_CAPACITY = 30;

		[SerializeField]
		private ItemTrait refillTrait = null;

		[SyncVar]
		private int labelAmount;

		[SyncVar]
		private string currentLabel;

		public void OnInputReceived(string input)
		{
			StartCoroutine(WaitToAllowInput());
			PlayerManager.LocalPlayerScript.PlayerNetworkActions.CmdRequestItemLabel(gameObject, input);
		}

		IEnumerator WaitToAllowInput()
		{
			yield return WaitFor.EndOfFrame;
			UIManager.IsInputFocus = false;
			UIManager.PreventChatInput = false;
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			labelAmount = LABEL_CAPACITY;
			currentLabel = "";
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			if (interaction.HandObject == null) return false;
			if (interaction.TargetObject.AttributesOrNull() == null) return false;
			if (HasWhiteListedComponents(interaction) == false) return false;

			//if(interaction.HandObject.Item().HasTrait(refillTrait)) return true; //Check for refill

			if (interaction.HandObject != gameObject) return false;

			return true;
		}

		private bool HasWhiteListedComponents(HandApply interaction)
		{
			return interaction.TargetObject.HasComponent<ClosetControl>() ||
			       interaction.TargetObject.HasComponent<ItemStorage>() ||
				   interaction.TargetObject.HasComponent<ItemAttributesV2>() ||
			       interaction.TargetObject.HasComponent<ObjectContainer>();
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (labelAmount == 0)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, "No labels left!");
				return;
			}

			if (currentLabel.Trim().Length == 0)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, "You haven't set a text yet.");
				return;
			}

			var item = interaction.TargetObject.AttributesOrNull();

			item.ServerSetArticleName(item.InitialName + " '" + currentLabel + "'");

			labelAmount--;

			Chat.AddActionMsgToChat(interaction, "You labeled " + item.InitialName + " as '" + currentLabel + "'.", interaction.Performer.Player().Name + " labeled " + item.InitialName + " as '" + currentLabel + "'.");
		}

		public bool WillInteract(InventoryApply interaction, NetworkSide side)
		{
			//must target this labeler to load rolls into
			if (!Validations.IsTarget(gameObject, interaction)) return false;
			//item to use must have refill trait
			if (!Validations.HasItemTrait(interaction, refillTrait)) return false;

			return true;
		}

		public void ServerPerformInteraction(InventoryApply interaction)
		{
			labelAmount = LABEL_CAPACITY;
			_ = Despawn.ServerSingle(interaction.UsedObject);
			Chat.AddExamineMsg(interaction.Performer, $"You insert the {interaction.UsedObject.AttributesOrNull().ArticleName.ToLower()} into the {gameObject.AttributesOrNull().InitialName.ToLower()}.");
		}

		public bool Interact(HandActivate interaction)
		{
			UIManager.Instance.TextInputDialog.ShowDialog("Set label text", OnInputReceived);

			return true;
		}

		public void SetLabel(string label)
		{
			currentLabel = label;

			if (currentLabel.Length > MAX_TEXT_LENGTH)
			{
				currentLabel = currentLabel.Substring(0, MAX_TEXT_LENGTH);
			}
		}
	}
}
