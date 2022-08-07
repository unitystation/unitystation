using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using NaughtyAttributes;


namespace Systems.Pipes
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
			else //(z > -45)
			{
				return (0);
			}

			return (0);
		}

		public static List<PipeData> GetConnectedPipes(List<PipeData> toPutInto, PipeData pipeData, Vector3Int location,
			Matrix locatedOn)
		{
			for (var i = 0; i < pipeData.Connections.Directions.Length; i++)
			{
				if (pipeData.Connections.Directions[i].Bool)
				{
					Vector3Int searchVector = Vector3Int.zero;
					switch (i)
					{
						case (int) PipeDirection.North:
							searchVector = Vector3Int.up;
							break;

						case (int) PipeDirection.East:
							searchVector = Vector3Int.right;
							break;

						case (int) PipeDirection.South:
							searchVector = Vector3Int.down;
							break;

						case (int) PipeDirection.West:
							searchVector = Vector3Int.left;
							break;
					}

					searchVector = location + searchVector;
					searchVector.z = 0;
					var pipesOnTile = locatedOn.GetPipeConnections(searchVector);
					foreach (var pipe in pipesOnTile)
					{
						if (ArePipeCompatible(pipeData, i, pipe, out var pipe1ConnectAndType))
						{
							pipe1ConnectAndType.Connected = pipe;
							toPutInto.Add(pipe);
						}
					}
				}
			}

			return (toPutInto);
		}

		public static PipeData GetPipeFromDirection(PipeData pipeData, Vector3Int location, PipeDirection direction, Matrix locatedOn)
		{
			location.z = 0;
			var pipesOnTile = locatedOn.GetPipeConnections(location);
			foreach (var pipe in pipesOnTile)
			{
				if (ArePipeCompatible(pipeData, (int)direction, pipe, out var _))
				{
					return pipe;
				}
			}

			return null;
		}

		public static bool ArePipeCompatible(PipeData pipe1, int Direction, PipeData pipe2,
			out ConnectAndType ConnectAndType)
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
						ConnectAndType = pipe1.Connections.Directions[Direction];
						return true;
					}
				}
			}

			ConnectAndType = null;
			return false;
		}

		public static bool IsPipeOutputTo(PipeData pipe1, PipeData pipe2)
		{
			var Data = pipe1.Connections.Directions[(int) PipesToDirections(pipe1, pipe2)];
			return Data.Bool && Data.PortType.HasFlag(OutputType.Output_Allowed);
		}

		public static bool CanEqualiseWith(PipeData pipe1, PipeData pipe2)
		{
			var PipeDirectio = PipesToDirections(pipe1, pipe2);
			int pipe2Direction = (int) PipeDirectio + 2;
			if (pipe2Direction > 3)
			{
				pipe2Direction -= 4;
			}

			return pipe2.Connections.Directions[pipe2Direction].PortType.HasFlag(OutputType.Can_Equalise_With);
		}

		public static PipeDirection PipesToDirections(PipeData pipe1, PipeData pipe2)
		{
			var vectorDifference = pipe2.MatrixPos - pipe1.MatrixPos;
			vectorDifference.z = 0; //TODO Tile map upgrade

			return VectorIntToPipeDirection(vectorDifference);
		}

		public static PipeDirection VectorIntToPipeDirection(Vector3Int vector3Int)
		{
			if (vector3Int == Vector3Int.up)
			{
				return PipeDirection.North;
			}

			if (vector3Int == Vector3Int.right)
			{
				return PipeDirection.East;
			}

			if (vector3Int == Vector3Int.down)
			{
				return PipeDirection.South;
			}

			if (vector3Int == Vector3Int.left)
			{
				return PipeDirection.West;
			}

			return PipeDirection.North;
		}

		public static MixAndVolume PipeOrNet(PipeData pipe)
		{
			if (pipe.NetCompatible && pipe.OnNet != null)
			{
				return (pipe.OnNet.mixAndVolume);
			}
			else
			{
				return (pipe.mixAndVolume);
			}
		}
	}

	[Serializable]
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

		public void PipeOffset(int OffSet)
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

		public ConnectAndType GetFlagToDirection(FlagLogic Flages)
		{
			foreach (var Direction in Directions)
			{
				if (Direction.flagLogic == Flages)
				{
					return Direction;
				}
			}

			return null;
		}
	}

	[Serializable]
	public class ConnectAndType
	{
		public bool Bool;

		[EnumFlags] public PipeType pipeType = PipeType.PipeRun;

		//This is ignored if its net compatible, Probably Should but I dont got time
		[EnumFlags] [FormerlySerializedAs("OutputType")]
		public OutputType PortType = OutputType.None;

		[NonSerialized]
		public PipeData Connected = null;


		public FlagLogic flagLogic = FlagLogic.None;


		public ConnectAndType Copy()
		{
			var Newone = new ConnectAndType();
			Newone.Bool = Bool;
			Newone.pipeType = pipeType;
			Newone.PortType = PortType;
			return (Newone);
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
		Output_Allowed = 1 << 0,
		Input_Allowed = 1 << 1,
		Something_Else = 1 << 2,
		Can_Equalise_With = Input_Allowed | Output_Allowed
	}

	public enum FlagLogic
	{
		None,
		UnfilteredOutput,
		FilteredOutput,
		InputOne,
		InputTwo
	}


	public enum CustomLogic
	{
		None,
		CoolingPipe,
	}
}
