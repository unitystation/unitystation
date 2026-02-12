using Chemistry;
using UnityEngine;
using US13.ChemistryComponents;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.Core.Lifecycle;
using US13.Items.Tool;
using US13.Items.Traits;
using US13.ScriptableObjects;
using US13.Systems.Fluids;
using US13.Tilemaps.Behaviours.Objects;
using Util;

namespace US13.Objects.Pipes.Devices
{
	public class ReservoirTank : MonoPipe, IServerDespawn , ICheckedInteractable<HandApply>
	{
		public ReagentContainer Container;

		[SerializeField]
		private ReagentMix initialContents = new ReagentMix();

		#region Lifecycle

		public override void OnSpawnServer(SpawnInfo info)
		{
			pipeData.PipeAction = new ReservoirAction();
			Container.SetIProvideReagentMix(pipeData);
			pipeData.GetMixAndVolume.SetReagentMix(initialContents.Clone());
			pipeData.GetMixAndVolume.SetVolume(Container.MaxCapacity);

			base.OnSpawnServer(info);
		}

		public override void OnDespawnServer(DespawnInfo info)
		{
			base.OnDespawnServer(info);
			Spawn.ServerPrefab(CommonPrefabs.Instance.Metal, this.GetComponent<RegisterObject>().WorldPositionServer, count: 20);
		}

		#endregion

		public override bool WillInteract(HandApply interaction, NetworkSide side )
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			if (!Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.Welder)) return false;

			return true;
		}

		public override void ServerPerformInteraction(HandApply interaction)
		{
			if (Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.Welder))
			{
				ToolUtils.ServerUseToolWithActionMessages(
						interaction, 10,
						"You start to deconstruct the tank...",
						$"{interaction.Performer.ExpensiveName()} starts to deconstruct the tank...",
						"You deconstruct the tank.",
						$"{interaction.Performer.ExpensiveName()} deconstructs the tank.",
						() => _ = Despawn.ServerSingle(gameObject));
			}
		}
	}
}
