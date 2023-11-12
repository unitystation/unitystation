using System;
using System.Collections.Generic;
using UnityEngine;
using Chemistry;
using Chemistry.Components;
using Systems.Atmospherics;
using Objects.Atmospherics;
using Items.Atmospherics;
using Logs;
using Tiles.Pipes;

namespace Systems.Pipes
{
	[Serializable]
	public class PipeData : IReagentMixProvider
	{
		public PipeLayer PipeLayer = PipeLayer.Second;
		public Connections Connections;
		public CustomLogic CustomLogic;
		public LiquidPipeNet OnNet;
		[HideInInspector] public PipeActions PipeAction;
		public bool NetCompatible = true;

		[HideInInspector] public MixAndVolume mixAndVolume = new MixAndVolume();

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

		[NonSerialized] public List<PipeData> ConnectedPipes = new List<PipeData>();
		[NonSerialized] public List<PipeData> Outputs = new List<PipeData>(); //Make sure to redirect to net if there is existence

		public PipeNode pipeNode;
		public MonoPipe MonoPipe;

		public bool Destroyed { get; private set; }

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

				Loggy.Log("Vector3Int null!!", Category.Pipes);
				return (Vector3Int.zero);
			}
		}

		public Matrix Matrix
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

				Loggy.Log("Matrix null!!", Category.Pipes);
				return (null);
			}
		}

		public virtual void OnEnable()
		{
			if (PipeAction != null)
			{
				PipeAction.pipeData = this;
			}

			Destroyed = false;

			AtmosManager.Instance.AddPipe(this);
			ConnectedPipes =
				PipeFunctions.GetConnectedPipes(ConnectedPipes, this, MatrixPos, Matrix);

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
						if (PipeFunctions.IsPipePortFlagTo(this, Pipe, OutputType.Output_Allowed) &&
						    PipeFunctions.CanEqualiseWith(this, Pipe))
						{
							Outputs.Add(Pipe);
						}

						//Shouldn't need to register outputs on Pipe Since it could handle itself
					}
					else
					{
						//What is connecting to is a Net
						if (PipeFunctions.IsPipePortFlagTo(this, Pipe, OutputType.Output_Allowed))
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
			AtmosManager.Instance.RemovePipe(this);
			Destroyed = true;
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
							Loggy.LogWarning("Pipe.OnNet == null", Category.Pipes);
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

		public ReagentMix GetReagentMix()
		{
			return PipeFunctions.PipeOrNet(this).GetReagentMix();
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

			var tileWorldPosition = MatrixManager.LocalToWorld(ZeroedLocation, Matrix).RoundToInt();
			var matrixInfo = MatrixManager.AtPoint(tileWorldPosition, true);

			MatrixManager.ReagentReact(ToSpill.Item1, tileWorldPosition, matrixInfo);

			MetaDataLayer metaDataLayer = matrixInfo.MetaDataLayer;
			if (pipeNode != null)
			{
				GasMix.TransferGas(pipeNode.IsOn.GasMix, ToSpill.Item2, ToSpill.Item2.Moles);
			}
			else
			{
				GasMix.TransferGas(Matrix.GetMetaDataNode(ZeroedLocation).GasMix, ToSpill.Item2, ToSpill.Item2.Moles);
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
					if (PipeFunctions.IsPipePortFlagTo(this, NewConnection, OutputType.Output_Allowed) &&
					    PipeFunctions.CanEqualiseWith(this, NewConnection))
					{
						pipe1Connection.Connected = NewConnection;
						Outputs.Add(NewConnection);
					}
				}
				else
				{
					//NewConnection is a Pipe net
					if (PipeFunctions.IsPipePortFlagTo(this, NewConnection, OutputType.Output_Allowed))
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
				Matrix4x4 matrix = Matrix.MetaTileMap.GetMatrix4x4(pipeNode.NodeLocation, LayerType.Pipe, true).GetValueOrDefault(Matrix4x4.identity);
				var pipe = Spawn.ServerPrefab(pipeNode.RelatedTile.SpawnOnDeconstruct,
											MatrixManager.LocalToWorld(pipeNode.NodeLocation, this.Matrix).To2().To3(),
											localRotation: PipeDeconstruction.QuaternionFromMatrix(matrix)).GameObject;

				var itempipe = pipe.GetComponent<PipeItemTile>();
				itempipe.Colour = Matrix.MetaTileMap.GetColour(pipeNode.NodeLocation, LayerType.Pipe, true).GetValueOrDefault(Color.white);
				itempipe.Setsprite();
				itempipe.rotatable.SetFaceDirectionRotationZ(PipeDeconstruction.QuaternionFromMatrix(matrix).eulerAngles.z);

				pipeNode.LocatedOn.TileChangeManager.MetaTileMap.RemoveTileWithlayer(pipeNode.NodeLocation, LayerType.Pipe);
				pipeNode.IsOn.PipeData.Remove(pipeNode);
				OnDisable();
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
