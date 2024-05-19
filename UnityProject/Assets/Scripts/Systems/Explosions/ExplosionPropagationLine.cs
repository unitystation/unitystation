using System;
using System.Collections.Generic;
using UnityEngine;

namespace Systems.Explosions
{
	public class ExplosionPropagationLine
	{
		public static List<ExplosionPropagationLine> PooledThis = new List<ExplosionPropagationLine>();

		public static ExplosionPropagationLine Getline()
		{
			if (PooledThis.Count > 0)
			{
				ExplosionPropagationLine line = PooledThis[0];
				PooledThis.RemoveAt(0);
				return (line);
			}
			else
			{
				return (new ExplosionPropagationLine());
			}
		}

		//Gets an XY direction of magnitude from a radian angle relative to the x axis
		//Simple version
		public static Vector2 GetXYDirection(float angle, float magnitude)
		{
			return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * magnitude;
		}


		int x0 = 0;
		int y0 = 0;
		int x1 = 0;
		int y1 = 0;
		private float Angle = 0;

		int dx = 0;
		int sx = 0;
		int dy = 0;
		int sy = 0;
		int err = 0;
		int e2 = 0;
		public float ExplosionStrength = 0;
		private bool InitialStep = true;

		private ExplosionNode NodeType;

		private bool IsAnyMatchingType(ExplosionNode[] expNodes, ExplosionNode nodeType)
		{
			foreach(ExplosionNode expNode in expNodes)
			{
				if (IsMatchingType(expNode, nodeType)) return true;
			}
			return false;
		}

		private bool IsMatchingType(ExplosionNode expNode, ExplosionNode nodeType)
		{
			if (expNode != null && nodeType != null) return expNode.GetType() == NodeType.GetType();
			return false;
		}

		private int FirstNullValue(ExplosionNode[] expNodes)
		{
			int count = 0;
			foreach(var val in expNodes)
			{
				if(val == null)
				{
					return count;
				}
				count++;
			}
			return -1;
		}

		public void SetUp(int X0, int Y0, int X1, int Y1, float InExplosionStrength, ExplosionNode nodeType)
		{
			x0 = X0;
			y0 = Y0;


			x1 = X1;
			y1 = Y1;

			Angle = 0; // (float) (Math.Atan2(x1 - x0, y1 - y0));
			InitialStep = true;
			dx = Math.Abs(x1 - x0);
			sx = x0 < x1 ? 1 : -1;

			dy = Math.Abs(y1 - y0);
			sy = y0 < y1 ? 1 : -1;

			err = (dx > dy ? dx : -dy) / 2;
			ExplosionStrength = InExplosionStrength;

			NodeType = nodeType;
		}

		public void Step()
		{
			if (ExplosionStrength <= 0)
			{
				Pool();
				return;
			}

			if (x0 == x1 && y0 == y1)
			{
				Pool();
				return;
			}

			Vector3Int WorldPSos = new Vector3Int(x0, y0, 0);
			MatrixInfo Matrix = MatrixManager.AtPoint(WorldPSos, CustomNetworkManager.IsServer);
			Vector3Int Local = WorldPSos.ToLocal(Matrix.Matrix).RoundToInt();
			MetaDataNode NodePoint = Matrix.MetaDataLayer.Get(Local); //Explosion node

			if (NodePoint != null)
			{
				if (IsAnyMatchingType(NodePoint.ExplosionNodes, NodeType) == false)
				{
					ExplosionNode expNode = NodeType.GenInstance();
					int fnull = FirstNullValue(NodePoint.ExplosionNodes);
					if (fnull >= 0) NodePoint.ExplosionNodes[fnull] = expNode;
					expNode.Initialise(Local, Matrix.Matrix);
				}

				foreach (ExplosionNode expNode in NodePoint.ExplosionNodes) //separating explosion nodes of our type from others, so we dont interfere with EMPs or whatever
				{
					if (IsMatchingType(expNode, NodeType))
					{
						expNode.AngleAndIntensity += GetXYDirection(Angle, ExplosionStrength);
						expNode.PresentLines.Add(this);
						ExplosionManager.CheckLocations.Add(expNode);
					}
				}
			}

			e2 = err;
			if (e2 > -dx)
			{
				err -= dy;
				x0 += sx;
			}

			if (e2 < dy)
			{
				err += dx;
				y0 += sy;
			}


			ExplosionManager.CheckLines.Add(this);
			if (InitialStep)
			{
				InitialStep = false;
				Angle = (float) (Math.Atan2(x1 - x0, y1 - y0));
			}
		}

		public void Pool()
		{
			PooledThis.Add(this);
		}
	}
}