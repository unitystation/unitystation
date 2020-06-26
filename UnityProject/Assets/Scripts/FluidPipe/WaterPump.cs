using System.Collections;
using System.Collections.Generic;
using Pipes;
using UnityEngine;

namespace Pipes
{
	//Improvements for the future
	//make it so it is less effective when pressure drop behind it
	public class WaterPump : MonoPipe,IServerDespawn , ICheckedInteractable<HandApply>
	{
		//Power stuff
		public int UnitPerTick = 100;
		public int PowerPercentage = 100;


		public void Start()
		{
			pipeData.PipeAction = new WaterPumpAction();
			base.Start();
		}

		public override void TickUpdate()
		{
			float AvailableReagents = 0;
			foreach (var Pipe in pipeData.ConnectedPipes)
			{
				if (pipeData.Outputs.Contains(Pipe) == false && CanEqualiseWithThis( Pipe))
				{
					var Data = PipeFunctions.PipeOrNet(Pipe);
					AvailableReagents += Data.Mix.Total;
				}
			}

			float TotalRemove = 0;
			if ((UnitPerTick * PowerPercentage) > AvailableReagents)
			{
				TotalRemove = AvailableReagents;
			}
			else
			{
				TotalRemove = (UnitPerTick * PowerPercentage);
			}

			foreach (var Pipe in pipeData.ConnectedPipes)
			{
				if (pipeData.Outputs.Contains(Pipe) == false && CanEqualiseWithThis(Pipe))
				{
					var Data = PipeFunctions.PipeOrNet(Pipe);
					Data.Mix.TransferTo(pipeData.mixAndVolume.Mix,
						(Data.Mix.Total / AvailableReagents) * TotalRemove);
				}
			}

			pipeData.mixAndVolume.EqualiseWithOutputs(pipeData.Outputs);
		}

		public bool CanEqualiseWithThis(PipeData Pipe)
		{
			if (Pipe.NetCompatible == false)
			{
				return PipeFunctions.CanEqualiseWith(this.pipeData, Pipe);
			}

			return true;
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
					"You start to deconstruct the WaterPump..",
					$"{interaction.Performer.ExpensiveName()} starts to deconstruct the WaterPump...",
					"You deconstruct the WaterPump",
					$"{interaction.Performer.ExpensiveName()} deconstruct the WaterPump.",
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
			Spawn.ServerPrefab(CommonPrefabs.Instance.Metal, this.GetComponent<RegisterObject>().WorldPositionServer, count: 25 );
		}


	}


}