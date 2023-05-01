using System.Collections.Generic;
using UnityEngine;
using ScriptableObjects;
using Systems.Pipes;


namespace Objects.Atmospherics
{
	//Improvements for the future
	//make it so it is less effective when pressure drop behind it
	public class WaterPump : MonoPipe,IServerDespawn , ICheckedInteractable<HandApply>
	{
		//Power stuff
		public int UnitPerTick = 100;
		public int PowerPercentage = 100;

		public override void OnSpawnServer(SpawnInfo info)
		{
			pipeData.PipeAction = new WaterPumpAction();
			base.OnSpawnServer(info);
		}

		public override void TickUpdate()
		{
			Vector2 AvailableReagents = new Vector2(0f,0f);
			foreach (var Pipe in pipeData.ConnectedPipes)
			{
				if (pipeData.Outputs.Contains(Pipe) == false && CanEqualiseWithThis( Pipe))
				{
					var Data = PipeFunctions.PipeOrNet(Pipe);
					AvailableReagents += Data.Total;
				}
			}

			Vector2 TotalRemove = Vector2.zero;
			if ((UnitPerTick * PowerPercentage) > AvailableReagents.x)
			{
				TotalRemove.x = AvailableReagents.x;
			}
			else
			{
				TotalRemove.x =  (UnitPerTick * PowerPercentage);
			}

			if ((UnitPerTick * PowerPercentage) > AvailableReagents.y)
			{
				TotalRemove.y = AvailableReagents.y;
			}
			else
			{
				TotalRemove.y =  (UnitPerTick * PowerPercentage);
			}


			foreach (var Pipe in pipeData.ConnectedPipes)
			{
				if (pipeData.Outputs.Contains(Pipe) == false && CanEqualiseWithThis(Pipe))
				{
					//TransferTo
					var Data = PipeFunctions.PipeOrNet(Pipe);
					Data.TransferTo(pipeData.mixAndVolume,
						(Data.Total / AvailableReagents) * TotalRemove);
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
				ToolUtils.ServerUseToolWithActionMessages(interaction, 10,
					"You start to deconstruct the WaterPump..",
					$"{interaction.Performer.ExpensiveName()} starts to deconstruct the WaterPump...",
					"You deconstruct the WaterPump",
					$"{interaction.Performer.ExpensiveName()} deconstruct the WaterPump.",
					() =>
					{
						Spawn.ServerPrefab(CommonPrefabs.Instance.Metal, gameObject.AssumedWorldPosServer(), count: 25);
						_ = Despawn.ServerSingle(gameObject);
					});
			}
		}

		/// <summary>
		/// is the function to denote that it will be pooled or destroyed immediately after this function is finished, Used for cleaning up anything that needs to be cleaned up before this happens
		/// </summary>
		//Cannot be used as electrical inheritance interferes with onserverdespawn
		//public override void OnDespawnServer(DespawnInfo info)
		//{
		//	base.OnDespawnServer(info);
		//	Spawn.ServerPrefab(CommonPrefabs.Instance.Metal, this.GetComponent<RegisterObject>().WorldPositionServer, count: 25 );
		//}
	}
}
