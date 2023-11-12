using System;
using UI.Core.Net;
using UnityEngine;
using Messages.Server;
using Mirror;
using Systems.Interaction;


namespace Objects
{
	/// <summary>
	/// Allows an object to have an associated network tab that pops up when clicked.
	/// If there are additional interactions that can be done on this object
	/// please ensure this component is placed below them, otherwise the tab open/close will
	/// be the interaction that always takes precedence.
	/// </summary>
	public class HasNetworkTab : NetworkBehaviour, ICheckedInteractable<HandApply>, IServerDespawn, ICheckedInteractable<AiActivate>
	{
		[NonSerialized]
		private GameObject playerInteracted;


		[Tooltip("Network tab to display.")]
		public NetTabType NetTabType = NetTabType.None;

		[SerializeField ]
		private bool aiInteractable = true;

		public event Action<GameObject> OnShowUI;

		/// <summary>
		/// This method simply tells the script what player last interacted, giving an reference to their gameobject
		/// </summary>
		public GameObject LastInteractedPlayer()
		{
			return playerInteracted;
		}

		[TargetRpc]
		private void InvokeEventOnClient(NetworkConnection target, GameObject player)
		{
			OnShowUI?.Invoke(player);
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side, AllowTelekinesis : false) == false)
				return false;
			playerInteracted = interaction.Performer;
			//interaction only works if hand is empty
			if (interaction.HandObject != null && interaction.IsAltClick == false)
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
			InvokeEventOnClient(interaction.PerformerPlayerScript.connectionToClient, interaction.Performer);
		}

		public void ServerPerformInteraction(PositionalHandApply interaction)
		{
			foreach (var validateNetTab in GetComponents<ICanOpenNetTab>())
			{
				if(validateNetTab.CanOpenNetTab(interaction.Performer, NetTabType)) continue;

				//If false block net tab opening
				return;
			}

			playerInteracted = interaction.Performer;
			TabUpdateMessage.Send(interaction.Performer, gameObject, NetTabType, TabAction.Open);
			InvokeEventOnClient(interaction.PerformerPlayerScript.connectionToServer, interaction.Performer);
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
			InvokeEventOnClient(interaction.PerformerMind.connectionToServer, interaction.Performer);
		}

		#endregion
	}
}
