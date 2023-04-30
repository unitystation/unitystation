using System.Collections.Generic;
using ScriptableObjects;
using Chemistry;
using Chemistry.Components;


namespace Objects.Atmospherics
{
	public class ReservoirTank : MonoPipe, IServerDespawn , ICheckedInteractable<HandApply>
	{
		public ReagentContainer Container;

		#region Lifecycle

		public override void OnSpawnServer(SpawnInfo info)
		{
			pipeData.PipeAction = new ReservoirAction();
			Container.SetIProvideReagentMix(pipeData);
			pipeData.GetMixAndVolume.GetGasMix().Volume = Container.MaxCapacity;
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
