using UnityEngine;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.Core.Lifecycle;
using US13.Items.Tool;
using US13.Items.Traits;

namespace US13.Systems.Construction
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
