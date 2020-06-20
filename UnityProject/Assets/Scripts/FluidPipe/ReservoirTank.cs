using System.Collections;
using System.Collections.Generic;
using Chemistry.Components;
using UnityEngine;


namespace Pipes
{
	public class ReservoirTank : MonoPipe,IServerDespawn , ICheckedInteractable<HandApply>
	{

		public ReagentContainer Container;
		private void Start()
		{
			pipeData.PipeAction = new ReservoirAction();
			base.Start();
		}



		public bool WillInteract( HandApply interaction, NetworkSide side )
		{

			if (!DefaultWillInteract.Default(interaction, side)) return false;
			if (!Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.Welder)) return false;

			return true;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.Welder))
			{
				ToolUtils.ServerUseToolWithActionMessages(interaction, 10,
					"You start to deconstruct the ReservoirTank..",
					$"{interaction.Performer.ExpensiveName()} starts to deconstruct the ReservoirTank...",
					"You deconstruct the ReservoirTank",
					$"{interaction.Performer.ExpensiveName()} deconstruct the ReservoirTank.",
					() =>
					{
						Despawn.ServerSingle(gameObject);
					});
			}
		}

		/// <summary>
		/// is the function to denote that it will be pooled or destroyed immediately after this function is finished, Used for cleaning up anything that needs to be cleaned up before this happens
		/// </summary>
		public override void OnDespawnServer(DespawnInfo info)
		{
			base.OnDespawnServer(info);
			Spawn.ServerPrefab(CommonPrefabs.Instance.Metal, this.GetComponent<RegisterObject>().WorldPositionServer, count: 10 );
		}

	}

}
