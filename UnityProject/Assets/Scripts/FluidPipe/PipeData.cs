using System;
using System.Collections;
using System.Collections.Generic;
using Atmospherics;
using Chemistry;
using UnityEngine;


namespace Pipes
{
	[System.Serializable]
	public class PipeData
	{
		public PipeLayer PipeLayer = PipeLayer.Second;
		public Connections Connections;
		public CustomLogic CustomLogic;
		public LiquidPipeNet OnNet;
		public PipeActions PipeAction;
		public bool NetCompatible = true;

		public MixAndVolume mixAndVolume = new MixAndVolume();

		public MixAndVolume GetMixAndVolume
		{
			get
			{
				if (NetCompatible)
				{
					return (OnNet.mixAndVolume);
				}
				else
				{
					return (mixAndVolume);
				}
			}
		}


		public List<PipeData> ConnectedPipes = new List<PipeData>();
		public List<PipeData> Outputs = new List<PipeData>(); //Make sure to redirect to net if there is existence

		public PipeNode pipeNode;
		public MonoPipe MonoPipe;

		public Vector3Int MatrixPos
		{
			get
			{
				if (pipeNode != null)
				{
					return (pipeNode.NodeLocation);
				}
				else if (MonoPipe != null)
				{
					return (MonoPipe.MatrixPos);
				}

				Logger.Log("Vector3Int null!!");
				return (Vector3Int.zero);
			}
		}

		public Matrix matrix
		{
			get
			{
				if (pipeNode != null)
				{
					return (pipeNode.LocatedOn);
				}
				else if (MonoPipe != null)
				{
					return (MonoPipe.Matrix);
				}

				Logger.Log("Matrix null!!");
				return (null);
			}
		}


		public virtual void OnEnable()
		{
			if (PipeAction != null)
			{
				PipeAction.pipeData = this;
			}

			AtmosManager.Instance.inGameNewPipes.Add(this);
			ConnectedPipes =
				PipeFunctions.GetConnectedPipes(ConnectedPipes, this, MatrixPos, matrix);

			foreach (var Pipe in ConnectedPipes)
			{
				Pipe.NetHookUp(this);
			}

			if (OnNet == null && NetCompatible)
			{
				OnNet = LiquidPipeNet.MakeNewNet(this);
			}

			foreach (var Pipe in ConnectedPipes)
			{
				Pipe.ConnectedAdd(this);
				if (NetCompatible == false)
				{
					//This is a special pipe
					//so Determine If neighbours special or net
					if (Pipe.NetCompatible == false)
					{
						//What is connecting to is a special pipe
						if (PipeFunctions.IsPipeOutputTo(this, Pipe) &&
						    PipeFunctions.CanEqualiseWith(this, Pipe))
						{
							Outputs.Add(Pipe);
						}

						//Shouldn't need to register outputs on Pipe Since it could handle itself
					}
					else
					{
						//What is connecting to is a Net
						if (PipeFunctions.IsPipeOutputTo(this, Pipe))
						{
							Outputs.Add(Pipe);
						}

						//Can it accept input?
						if (this.Connections.Directions[(int) PipeFunctions.PipesToDirections(this, Pipe)].PortType
							.HasFlag(OutputType.Can_Equalise_With))
						{
							Pipe.OnNet.AddEqualiseWith(this);
						}
					}
				}
			}
		}

		public virtual void OnDisable()
		{
			AtmosManager.Instance.inGameNewPipes.Remove(this);
			foreach (var Pipe in ConnectedPipes)
			{
				if(Pipe == null) continue;

				Pipe.ConnectedRemove(this);

				foreach (var Connection in Connections.Directions)
				{
					if (Connection.Connected == Pipe)
					{
						Connection.Connected = null;
					}
				}

				if (NetCompatible == false)
				{
					//This is a special Pipe
					if (Pipe.NetCompatible == false)
					{
						Outputs.Remove(Pipe);
					}
					else
					{
						Outputs.Remove(Pipe);
						//Removes itself if it is connected to a network and its a output for the network

						// this one probably require more work than just null check
						if(Pipe.OnNet == null)
							Logger.LogWarning("Pipe.OnNet == null", Category.Atmos);
						else
							Pipe.OnNet.RemoveEqualiseWith(this);
					}
				}
			}

			ConnectedPipes.Clear();
			//managing external listeners
			if (NetCompatible && OnNet != null)
			{
				OnNet.RemovePipe(this);
			}
			else
			{
				SpillContent(mixAndVolume.Take(mixAndVolume, false));
			}


			//MatrixManager.ReagentReact(mixAndVolume.Mix, MatrixPos); //TODO AAAAAAAA Get the correct location
		}

		public void SpillContent(Tuple<ReagentMix, GasMix> ToSpill)
		{
			if (pipeNode == null && MonoPipe == null ) return;

			Vector3Int ZeroedLocation = Vector3Int.zero;
			if (pipeNode != null)
			{
				ZeroedLocation =  pipeNode.NodeLocation;
			}
			else
			{
				ZeroedLocation = MonoPipe.MatrixPos;
			}

			ZeroedLocation.z = 0;
			var tileWorldPosition = MatrixManager.LocalToWorld(ZeroedLocation, matrix).RoundToInt();
			MatrixManager.ReagentReact(ToSpill.Item1, tileWorldPosition);
			MetaDataLayer metaDataLayer = MatrixManager.AtPoint(tileWorldPosition, true).MetaDataLayer;
			if (pipeNode != null)
			{
				pipeNode.IsOn.GasMix += ToSpill.Item2;
			}
			else
			{
				matrix.GetMetaDataNode(ZeroedLocation).GasMix += ToSpill.Item2;
			}
			metaDataLayer.UpdateSystemsAt(ZeroedLocation, SystemType.AtmosSystem);
		}

		public void NetHookUp(PipeData NewConnection)
		{
			if (NewConnection.NetCompatible && NetCompatible)
			{
				OnNet?.AddPipe(NewConnection);
			}
		}

		public void ConnectedAdd(PipeData NewConnection)
		{
			ConnectedPipes.Add(NewConnection);
			var pipe1Connection =
				this.Connections.Directions[(int) PipeFunctions.PipesToDirections(this, NewConnection)];
			pipe1Connection.Connected = NewConnection;


			if (NetCompatible == false)
			{
				//This is a special pipe
				if (NewConnection.NetCompatible == false)
				{
					//NewConnection is a special pipe
					if (PipeFunctions.IsPipeOutputTo(this, NewConnection) &&
					    PipeFunctions.CanEqualiseWith(this, NewConnection))
					{
						pipe1Connection.Connected = NewConnection;
						Outputs.Add(NewConnection);
					}
				}
				else
				{
					//NewConnection is a Pipe net
					if (PipeFunctions.IsPipeOutputTo(this, NewConnection))
					{
						//An input to the pipe net it does not need to be recorded
						Outputs.Add(NewConnection);
					}

					if (this.Connections.Directions[(int) PipeFunctions.PipesToDirections(this, NewConnection)].PortType
						.HasFlag(OutputType.Can_Equalise_With))
					{
						NewConnection.OnNet.AddEqualiseWith(this);
					}
				}
			}
		}

		public void ConnectedRemove(PipeData OldConnection)
		{
			ConnectedPipes.Remove(OldConnection);

			if (NetCompatible == false)
			{
				Outputs.Remove(OldConnection);
			}

			foreach (var Connection in Connections.Directions)
			{
				if (Connection.Connected == OldConnection)
				{
					Connection.Connected = null;
				}
			}

			//What about net outputs then That should be handle as part of the Reconstruction of the net
		}

		public void SetUp(PipeTile PipeTile, int RotationOffset)
		{
			Connections = PipeTile.Connections.Copy();
			Connections.Rotate(RotationOffset);
			PipeLayer = PipeTile.PipeLayer;
			NetCompatible = PipeTile.NetCompatible;
			mixAndVolume.SetVolume(PipeTile.Volume);
			switch (PipeTile.CustomLogic)
			{
				case CustomLogic.None:
					//Awaiting custom logic
					//PipeAction = new Action(); //However
					break;
			}


			CustomLogic = PipeTile.CustomLogic;
		}

		public string ToAnalyserExamineString()
		{
			var ToLog = "";
			if (OnNet != null)
			{
				ToLog = ToLog + "On net > " + OnNet.ToAnalyserExamineString() + "\n";
			}
			else
			{
				ToLog = ToLog + " Mix " + mixAndVolume.ToString() + "\n";
			}

			return ToLog;
		}

		public void DestroyThis()
		{
			if (MonoPipe == null)
			{
				var Transform = matrix.UnderFloorLayer.GetMatrix4x4(pipeNode.NodeLocation, pipeNode.RelatedTile);
				var pipe = Spawn.ServerPrefab(pipeNode.RelatedTile.SpawnOnDeconstruct,  MatrixManager.LocalToWorld(pipeNode.NodeLocation,matrix),
					localRotation: PipeDeconstruction.QuaternionFromMatrix(Transform)).GameObject;
				var itempipe = pipe.GetComponent<PipeItemTile>();
				itempipe.Colour = matrix.UnderFloorLayer.GetColour(pipeNode.NodeLocation, pipeNode.RelatedTile);
				itempipe.Setsprite();
				//matrix.RemoveUnderFloorTile(pipeNode.NodeLocation,pipeNode.RelatedTile);
				pipeNode.LocatedOn.RemoveUnderFloorTile(pipeNode.NodeLocation, pipeNode.RelatedTile);

				pipeNode.IsOn.PipeData.Remove(pipeNode);
				this.OnDisable();
			}
		}

		public override string ToString()
		{
			var ToLog = "ConnectedPipes > " + ConnectedPipes.Count + "\n";
			ToLog = ToLog + "Outputs > " + Outputs.Count + "\n";
			if (OnNet != null)
			{
				ToLog = ToLog + "On net > " + OnNet.ToString() + "\n";
			}
			else
			{
				ToLog = ToLog + " Mix " + mixAndVolume.ToString() + "\n";
			}

			return ToLog;
		}

		public void LiquidFindNetWork()
		{
			if (NetCompatible)
			{
				foreach (var pipe in ConnectedPipes)
				{
					if (pipe.NetCompatible)
					{
						if (pipe.OnNet != null)
						{
							pipe.OnNet.AddPipe(this);
						}
					}
				}

				if (OnNet == null)
				{
					OnNet = LiquidPipeNet.MakeNewNet(this);
				}
			}
		}


		public virtual void TickUpdate()
		{
			if (PipeAction != null)
			{
				PipeAction.TickUpdate();
			}
		}
	}
}