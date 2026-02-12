using UnityEngine;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.Core.Lifecycle;
using Util;

namespace US13.Items.Weapons
{
	public class SyndicateBombBecon : MonoBehaviour, IInteractable<HandActivate>
	{
		[SerializeField] private GameObject bomb;
		[SerializeField] private GameObject remoteDevice;

		public void ServerPerformInteraction(HandActivate interaction)
		{
			Spawn.ServerPrefab(bomb, interaction.Performer.AssumedWorldPosServer());
			Spawn.ServerPrefab(remoteDevice, interaction.Performer.AssumedWorldPosServer());
			_ = Despawn.ServerSingle(gameObject);
		}
	}
}