using System;
using System.Collections.Generic;
using Chemistry;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Serialization;


namespace Pipes
{
	public static class PipeFunctions
	{


		//Even how Funny it may be to have Oddly rotated tiles It would be a pain
		public static int GetOffsetAngle(float z)
		{
			if (z > 255)
			{
				return (270);
			}
			else if (z > 135)
			{
				return (180);
			}
			else if (z > 45)
			{
				return (90);
			}
			else if (z > -45)
			{
				return (0);
			}
			else if (z > -135)
			{
				return (90);
			}
			else if (z > -255)
			{
				return (180);
			}
			else if (z > -345)
			{
				return (270);
			}
			else if (z < -345)
			{
				return (0);
			}

			return (0);
		}


		public static List<PipeData> GetConnectedPipes(List<PipeData> ToPutInto, PipeData pipeData, Vector3Int Location,
			Matrix LocatedOn)
		{
			for (var i = 0; i < pipeData.Connections.Directions.Length; i++)
			{
				if (pipeData.Connections.Directions[i].Bool)
				{
					Vector3Int SearchVector = Vector3Int.zero;
					switch (i)
					{
						case (int) PipeDirection.North:
							SearchVector = Vector3Int.up;
							break;

						case (int) PipeDirection.East:
							SearchVector = Vector3Int.right;
							break;

						case (int) PipeDirection.South:
							SearchVector = Vector3Int.down;
							break;

						case (int) PipeDirection.West:
							SearchVector = Vector3Int.left;
							break;
					}

					SearchVector = Location + SearchVector;
					SearchVector.z = 0;
					var PipesOnTile = LocatedOn.GetPipeConnections(SearchVector);
					foreach (var pipe in PipesOnTile)
					{
						if (ArePipeCompatible(pipeData, i, pipe))
						{
							ToPutInto.Add(pipe);
						}
					}
				}
			}

			return (ToPutInto);
		}

		public static bool ArePipeCompatible(PipeData pipe1, int Direction, PipeData pipe2)
		{
			if (pipe1.PipeType == pipe2.PipeType)
			{
				if (pipe1.PipeLayer == pipe2.PipeLayer)
				{
					int pipe2Direction = Direction + 2;
					if (pipe2Direction > 3)
					{
						pipe2Direction -= 4;
					}
					if (pipe2.Connections.Directions[pipe2Direction].Bool)
					{
						if (pipe2.Connections.Directions[pipe2Direction].pipeType
							.HasFlag(pipe1.Connections.Directions[Direction].pipeType))
						{
							return true;
						}
					}
				}
			}

			return false;
		}

		public static bool IsPipeOutputTo(PipeData pipe1, PipeData pipe2)
		{
			var Data = pipe1.Connections.Directions[(int)PipesToDirections(pipe1,pipe2)];
			return Data.Bool && Data.PortType.HasFlag( OutputType.Output_Allowed );
		}

		public static bool CanEqualiseWith(PipeData pipe1, PipeData pipe2)
		{
			var PipeDirectio = PipesToDirections(pipe1, pipe2);
			int pipe2Direction = (int)PipeDirectio + 2;
			if (pipe2Direction > 3)
			{
				pipe2Direction -= 4;
			}
			return pipe2.Connections.Directions[pipe2Direction].PortType.HasFlag(OutputType.Can_Equalise_With);
		}


		public static PipeDirection PipesToDirections(PipeData pipe1, PipeData pipe2)
		{
			var VectorDifference = pipe2.MatrixPos - pipe1.MatrixPos;
			if (VectorDifference == Vector3Int.up)
			{
				return  PipeDirection.North;
			}
			else if (VectorDifference == Vector3Int.right)
			{
				return PipeDirection.East;
			}
			else if (VectorDifference == Vector3Int.down)
			{
				return  PipeDirection.South;
			}
			else if (VectorDifference == Vector3Int.left)
			{
				return PipeDirection.West;
			}

			return PipeDirection.North;
		}

		public static MixAndVolume PipeOrNet(PipeData pipe)
		{
			if (pipe.NetCompatible)
			{
				return (pipe.OnNet.mixAndVolume);
			}
			else
			{
				return (pipe.mixAndVolume);
			}
		}
	}
	[System.Serializable]
	public class Connections
	{
		public ConnectAndType[] Directions = new ConnectAndType[4];

		public void Rotate(float InRotate)
		{
			if (InRotate == 0)
			{
			}
			else
			{
				if (InRotate > 181)
				{
					PipeOffset(1);
				}
				else if (InRotate > 91)
				{
					PipeOffset(2);
				}
				else
				{
					PipeOffset(3);
				}
			}
		}

		private void PipeOffset(int OffSet)
		{
			var NewConnectionss = new ConnectAndType[4];


			for (int i = 0; i < Directions.Length; i++)
			{
				int Location = i + OffSet;
				if (Location > 3)
				{
					Location -= 4;
				}

				NewConnectionss[Location] = Directions[i];
			}

			Directions = NewConnectionss;
		}

		public override string ToString()
		{
			string DD = "";
			for (int i = 0; i < Directions.Length; i++)
			{
				DD += ("," + Directions[i]);
			}


			return DD + "\n";
		}

		public static Connections CopyFrom(Connections InConnection)
		{
			var newone = new Connections();
			for (int i = 0; i < InConnection.Directions.Length; i++)
			{
				newone.Directions[i] = InConnection.Directions[i].Copy();
			}

			return (newone);
		}

		public Connections Copy()
		{
			return (CopyFrom(this));
		}
	}

	[System.Serializable]
	public class ConnectAndType
	{
		public bool Bool;

		[EnumFlags]
		public PipeType pipeType = PipeType.PipeRun;

		//This is ignored if its net compatible, Probably Should but I dont got time
		[EnumFlags]
		[FormerlySerializedAs("OutputType")] public OutputType PortType = OutputType.None;

		public ConnectAndType Copy()
		{
			var Newone = new ConnectAndType();
			Newone.Bool = Bool;
			Newone.pipeType = pipeType;
			Newone.PortType = PortType;
			return (Newone);
		}
	}

	[System.Serializable]
	public class MixAndVolume
	{
		public float Volume = 50;
		public ReagentMix Mix = new ReagentMix();

		public float Density()
		{
			return (Mix.Total / Volume);
		}

		public void EqualiseWith(PipeData Another)
		{
			float TotalVolume = Volume + PipeFunctions.PipeOrNet(Another).Volume;
			float TotalReagents = Mix.Total + PipeFunctions.PipeOrNet(Another).Mix.Total;
			float TargetDensity = TotalReagents / TotalVolume;

			float thisAmount = TargetDensity * Volume;
			float AnotherAmount = TargetDensity * PipeFunctions.PipeOrNet(Another).Volume;

			if (thisAmount > Mix.Total)
			{
				PipeFunctions.PipeOrNet(Another).Mix.TransferTo(Mix, PipeFunctions.PipeOrNet(Another).Mix.Total-AnotherAmount);
			}
			else
			{
				this.Mix.TransferTo(PipeFunctions.PipeOrNet(Another).Mix, AnotherAmount- PipeFunctions.PipeOrNet(Another).Mix.Total);
			}
		}

		public void EqualiseWithMultiple( List<PipeData>  others)
		{
			float TotalVolume = Volume;
			foreach (var Pipe in others)
			{
				TotalVolume += PipeFunctions.PipeOrNet(Pipe).Volume;
			}

			float TotalReagents = Mix.Total;
			foreach (var Pipe in others)
			{
				TotalReagents += PipeFunctions.PipeOrNet(Pipe).Mix.Total;
			}
			float TargetDensity = TotalReagents / TotalVolume;


			foreach (var Pipe in others)
			{
				PipeFunctions.PipeOrNet(Pipe).Mix.TransferTo(Mix, PipeFunctions.PipeOrNet(Pipe).Mix.Total);
			}

			foreach (var Pipe in others)
			{
				Mix.TransferTo(PipeFunctions.PipeOrNet(Pipe).Mix,  TargetDensity*PipeFunctions.PipeOrNet(Pipe).Volume);
			}
		}

		public void EqualiseWithOutputs(List<PipeData> others)
		{
			bool DifferenceInPressure = false;
			float density = this.Density();
			foreach (var Pipe in others)
			{
				if (Math.Abs(PipeFunctions.PipeOrNet(Pipe).Density() - density) > 0.001f)
				{
					DifferenceInPressure = true;
					break;
				}
			}

			if (DifferenceInPressure)
			{
				if (others.Count > 1)
				{
					EqualiseWithMultiple(others);
				}
				else if (others.Count > 0)
				{
					EqualiseWith(others[0]);
				}
			}
		}

		public override string ToString()
		{
			return "Volume > " + Volume + " Mix > " + Mix.ToString();
		}
	}

	public enum PipeDirection
	{
		North = 0,
		East = 1,
		South = 2,
		West = 3,
	}

	[Flags]
	public enum PipeType
	{
		None = 0,
		PipeRun = 1 << 0,
		CoolingPipe = 1 << 1,
		//Used for stopping cooling pipes to connect to usual pipes
	}

	public enum CorePipeType //Would be nice to integrate into one at some point but Its hard to work out how to implement Liquids into a gas simulator : P
	{
		Unset,
		AtmosPipe,
		WaterPipe,
		//Stuff like vents and things
	}

	public enum PipeLayer
	{
		First = 0,
		Second = 1,
		Third = 2
	}

	[Flags]
	//Used to keeping track of output connections
	public enum OutputType
	{
		None = 0,
		Output_Allowed  = 1 << 0,
		Input_Allowed = 1 << 1,
		Something_Else = 1 << 2,
		Can_Equalise_With = Input_Allowed | Output_Allowed

	}

	public enum CustomLogic
	{
		None,
		CoolingPipe,

	}

}