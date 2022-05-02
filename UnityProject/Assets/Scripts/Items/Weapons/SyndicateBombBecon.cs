using UnityEngine;

namespace Items.Weapons
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