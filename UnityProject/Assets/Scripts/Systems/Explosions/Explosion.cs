using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
			Logger.Log("Setting up line, x0: " + X0.ToString() + "; y0: " + Y0.ToString() + "; x1: " + X1.ToString() + "; y1: " + Y1.ToString() + "; strength: " + InExplosionStrength.ToString() + "; node: " + nodeType.GetType().ToString());
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
				if (!NodePoint.ExplosionNodes.Any(p => p.GetType() == NodeType.GetType()))
				{
					ExplosionNode expNode = (ExplosionNode)Activator.CreateInstance(NodeType.GetType());
					NodePoint.ExplosionNodes.Add(expNode);
					expNode.Initialise(Local, Matrix.Matrix);
				}

				foreach (ExplosionNode expNode in NodePoint.ExplosionNodes) //separating explosion nodes of our type from others, so we dont interfere with EMPs or whatever
				{
					if (expNode.GetType() == NodeType.GetType())
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

	public class Explosion
	{
		//Function to check CheckLocations

		public class ExplosionData
		{
			public HashSet<Vector2Int> CircleCircumference = new HashSet<Vector2Int>();
		}

		public static void StartExplosion(Vector3Int WorldPOS, float strength, ExplosionNode nodeType = null, int fixedRadius = -1, int fixedShakingStrength = -1)
		{
			if (!(nodeType is ExplosionNode))
			{
				nodeType = new ExplosionNode();
			}

			int Radius = 0;
			if (fixedRadius <= 0)
            {
				Radius = (int)Math.Round(strength / (Math.PI * 75));
			}
            else
            {
				Radius = fixedRadius;
            }
			if (Radius > 150)
			{
				Radius = 150;
			}

			byte ShakingStrength = 0;
			if (fixedShakingStrength <= 0 || fixedShakingStrength > 255)
            {
				ShakingStrength = 25;
				if (strength > 800)
				{
					ShakingStrength = 75;
				}
				else if (strength > 8000)
				{
					ShakingStrength = 125;
				}
				else if (strength > 80000)
				{
					ShakingStrength = 255;
				}
			}
            else
            {
				ShakingStrength = (byte)fixedShakingStrength;
            }

			ExplosionUtils.PlaySoundAndShake(WorldPOS, ShakingStrength, Radius / 20, nodeType.CustomSound);

			//Generates the conference
			var explosionData = new ExplosionData();
			circleBres(explosionData, WorldPOS.x, WorldPOS.y, Radius);
			float InitialStrength = strength / explosionData.CircleCircumference.Count;

			foreach (var ToPoint in explosionData.CircleCircumference)
			{
				var Line = ExplosionPropagationLine.Getline();
				Line.SetUp(WorldPOS.x, WorldPOS.y, ToPoint.x, ToPoint.y, InitialStrength, nodeType);
				Line.Step();
			}
		}


		//https://www.geeksforgeeks.org/bresenhams-circle-drawing-algorithm/
		// Function for circle-generation
		// using Bresenham's algorithm
		static void circleBres(ExplosionData explosionData, int xc, int yc, int r)
		{
			int x = 0, y = r;
			int d = 3 - 2 * r;
			drawCircle(explosionData, xc, yc, x, y);
			while (y >= x)
			{
				// for each pixel we will
				// draw all eight pixels

				x++;

				// check for decision parameter
				// and correspondingly
				// update d, x, y
				if (d > 0)
				{
					y--;
					d = d + 4 * (x - y) + 10;
				}
				else
					d = d + 4 * x + 6;

				drawCircle(explosionData, xc, yc, x, y);
				//delay(50);
			}
		}

		// Function to put Locations
		// at subsequence points
		static void drawCircle(ExplosionData explosionData, int xc, int yc, int x, int y)
		{
			explosionData.CircleCircumference.Add(new Vector2Int(xc + x, yc + y));
			explosionData.CircleCircumference.Add(new Vector2Int(xc - x, yc + y));
			explosionData.CircleCircumference.Add(new Vector2Int(xc + x, yc - y));
			explosionData.CircleCircumference.Add(new Vector2Int(xc - x, yc - y));
			explosionData.CircleCircumference.Add(new Vector2Int(xc + y, yc + x));
			explosionData.CircleCircumference.Add(new Vector2Int(xc - y, yc + x));
			explosionData.CircleCircumference.Add(new Vector2Int(xc + y, yc - x));
			explosionData.CircleCircumference.Add(new Vector2Int(xc - y, yc - x));
		}
	}
}