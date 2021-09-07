using System;
using UI.Core.Net;
using UnityEngine;
using Core.Editor.Attributes;
using Messages.Server;
using Systems.Interaction;


namespace Objects
{
	/// <summary>
	/// Allows an object to have an associated network tab that pops up when clicked.
	/// If there are additional interactions that can be done on this object
	/// please ensure this component is placed below them, otherwise the tab open/close will
	/// be the interaction that always takes precedence.
	/// </summary>
	public class HasNetworkTab : MonoBehaviour, ICheckedInteractable<HandApply>, IServerDespawn, ICheckedInteractable<AiActivate>
	{
		[NonSerialized]
		private GameObject playerInteracted;

		[PrefabModeOnly]
		[Tooltip("Network tab to display.")]
		public NetTabType NetTabType = NetTabType.None;

		[SerializeField, PrefabModeOnly]
		private bool aiInteractable = true;

		/// <summary>
		/// This method simply tells the script what player last interacted, giving an reference to their gameobject
		/// </summary>
		public GameObject LastInteractedPlayer()
		{
			return playerInteracted;
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (!DefaultWillInteract.Default(interaction, side))
				return false;
			playerInteracted = interaction.Performer;
			//interaction only works if hand is empty
			if (interaction.HandObject != null)
			{ return false; }

			return true;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			foreach (var validateNetTab in GetComponents<ICanOpenNetTab>())
			{
				if(validateNetTab.CanOpenNetTab(interaction.Performer, NetTabType)) continue;

				//If false block net tab opening
				return;
			}

			playerInteracted = interaction.Performer;
			TabUpdateMessage.Send(interaction.Performer, gameObject, NetTabType, TabAction.Open);
		}

		public void OnDespawnServer(DespawnInfo info)
		{
			NetworkTabManager.Instance.RemoveTab(gameObject, NetTabType);
		}

		#region Ai interaction

		public bool WillInteract(AiActivate interaction, NetworkSide side)
		{
			if (aiInteractable == false) return false;

			//Normal click to open tab
			if (interaction.ClickType != AiActivate.ClickTypes.NormalClick) return false;

			if (DefaultWillInteract.AiActivate(interaction, side) == false) return false;

			return true;
		}

		public void ServerPerformInteraction(AiActivate interaction)
		{
			foreach (var validateNetTab in GetComponents<ICanOpenNetTab>())
			{
				if(validateNetTab.CanOpenNetTab(interaction.Performer, NetTabType)) continue;

				//If false block net tab opening
				return;
			}

			playerInteracted = interaction.Performer;
			TabUpdateMessage.Send(interaction.Performer, gameObject, NetTabType, TabAction.Open);
		}

		#endregion

	}
}
