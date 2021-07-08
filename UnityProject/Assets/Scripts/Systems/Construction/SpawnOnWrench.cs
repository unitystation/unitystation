using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Objects
{
	public class SpawnOnWrench : MonoBehaviour, ICheckedInteractable<HandApply>
	{
		[Tooltip("What is spawned when you wrench it")]
		[SerializeField]
		private GameObject toSpawn = null;

		public GameObject gametoSpawnObject => toSpawn;

		public virtual bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			if (interaction.TargetObject != gameObject) return false;
			if (interaction.HandObject == null) return false;

			return true;
		}

		public virtual void ServerPerformInteraction(HandApply interaction)
		{
			if (Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.Wrench))
			{
				ToolUtils.ServerPlayToolSound(interaction);
				Spawn.ServerPrefab(toSpawn, transform.position);
				_ = Despawn.ServerSingle(gameObject);
			}
		}
	}
}
