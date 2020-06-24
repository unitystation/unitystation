using System.Collections;
using System.Collections.Generic;
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
		public CorePipeType PipeType;
		public LiquidPipeNet OnNet;
		public PipeActions PipeAction;
		public bool NetCompatible = true;

		public MixAndVolume mixAndVolume = new MixAndVolume();

		public const float Takeaway_Pressure = (50 * (273.15f + 20f)); //TODO humm, Could be based on air pressure

		public float Pressure => CalculateCurrentPressure();

		//Pressure = mix * 20c (in k)) - (Volume * 20c (in k))
		public float MaxPressure = 15805.5f;

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
						if (PipeFunctions.IsPipeOutputTo(this, Pipe) && PipeFunctions.CanEqualiseWith(this, Pipe))
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
				Pipe.ConnectedRemove(this);
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
						Pipe.OnNet.RemoveEqualiseWith(this);
					}
				}
			}

			//managing external listeners
			if (NetCompatible && OnNet != null)
			{
				OnNet.RemovePipe(this);
			}
			//MatrixManager.ReagentReact(mixAndVolume.Mix, MatrixPos); //TODO AAAAAAAA Get the correct location
		}

		public void NetHookUp(PipeData NewConnection)
		{
			if (NewConnection.NetCompatible && NetCompatible)
			{
				OnNet.AddPipe(NewConnection);
			}
		}

		public void ConnectedAdd(PipeData NewConnection)
		{
			ConnectedPipes.Add(NewConnection);
			if (NetCompatible == false)
			{
				//This is a special pipe
				if (NewConnection.NetCompatible == false)
				{
					//NewConnection is a special pipe
					if (PipeFunctions.IsPipeOutputTo(this, NewConnection) && PipeFunctions.CanEqualiseWith(this, NewConnection))
					{
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

			//What about net outputs then That should be handle as part of the Reconstruction of the net
		}

		public float CalculateCurrentPressure()
		{
			return ((mixAndVolume.Mix.Total * mixAndVolume.Mix.Temperature) - Takeaway_Pressure);
		}

		public void SetUp(PipeTile PipeTile, int RotationOffset)
		{
			Connections = PipeTile.Connections.Copy();
			Connections.Rotate(RotationOffset);

			PipeType = PipeTile.PipeType;
			PipeLayer = PipeTile.PipeLayer;
			NetCompatible = PipeTile.NetCompatible;
			switch (PipeTile.CustomLogic)
			{
				case CustomLogic.None:
					//Awaiting custom logic
					//PipeAction = new Action(); //However
					break;
			}


			CustomLogic = PipeTile.CustomLogic;
		}


		public override string ToString()
		{
			var ToLog = "Connections > " + Connections + " PipeType > " + PipeType + "\n";
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
				/*if (OnNet == null)
				{
					OnNet = LiquidPipeNet.MakeNewNet(this);
				}*/

				foreach (var pipe in ConnectedPipes)
				{
					if (pipe.NetCompatible)
					{
						OnNet.AddPipe(pipe);
					}
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