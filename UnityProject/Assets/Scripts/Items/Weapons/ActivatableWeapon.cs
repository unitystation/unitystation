using System;
using Mirror;
using UnityEngine;

namespace Weapons.ActivatableWeapons
{
	public class ActivatableWeapon : NetworkBehaviour, ICheckedInteractable<HandActivate>
	{
		[SyncVar(hook = nameof(SyncState))] private bool isActive = false;

		public bool IsActive => isActive;

		public Action<GameObject> ServerOnActivate;
		public Action<GameObject> ServerOnDeactivate;

		public Action ClientOnActivate;
		public Action ClientOnDeactivate;

		public void SyncState(bool oldState, bool newState)
		{
			isActive = newState;

			if (isActive)
			{
				ClientOnActivate?.Invoke();
			}
			else
			{
				ClientOnDeactivate?.Invoke();
			}
		}

		public bool WillInteract(HandActivate interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			return true;
		}

		public void ServerPerformInteraction(HandActivate interaction)
		{
			if (isActive)
			{
				ServerOnDeactivate?.Invoke(interaction.Performer);
			}
			else
			{
				ServerOnActivate?.Invoke(interaction.Performer);
			}
			SyncState(isActive, !isActive);

		}
	}
}
