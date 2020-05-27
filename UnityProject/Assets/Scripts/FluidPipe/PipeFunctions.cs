using System.Collections;
using System.Collections.Generic;
using Chemistry;
using MLAgents.CommunicatorObjects;
using UnityEditor.Experimental.GraphView;
using UnityEngine;


namespace Pipes
{
	public static class PipeFunctions
	{
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
					Logger.Log("Direction" + Direction);
					if (pipe2Direction > 3)
					{
						pipe2Direction -= 4;
					}

					Logger.Log("pipe2Direction" + pipe2Direction);
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
			var VectorDifference = pipe2.MatrixPos - pipe1.MatrixPos;
			if (VectorDifference == Vector3Int.up)
			{
				return pipe1.OutConnections.Directions[(int) PipeDirection.North].Bool;
			}
			else if (VectorDifference == Vector3Int.right)
			{
				return pipe1.OutConnections.Directions[(int) PipeDirection.East].Bool;
			}
			else if (VectorDifference == Vector3Int.down)
			{
				return pipe1.OutConnections.Directions[(int) PipeDirection.South].Bool;
			}
			else if (VectorDifference == Vector3Int.left)
			{
				return pipe1.OutConnections.Directions[(int) PipeDirection.West].Bool;
			}

			return false;
		}


		public static PipeDirection PipesToDuctions(PipeData pipe1, PipeData pipe2)
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
}