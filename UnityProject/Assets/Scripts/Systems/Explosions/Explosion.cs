using System;
using System.Collections.Generic;
using HealthV2;
using Player;
using Systems.Score;
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

	public class Explosion
	{
		//Function to check CheckLocations

		public class ExplosionData
		{
			public HashSet<Vector2Int> CircleCircumference = new HashSet<Vector2Int>();
		}

		public static void StartExplosion(Vector3Int WorldPOS, float strength, ExplosionNode nodeType = null,
			int fixedRadius = -1, int fixedShakingStrength = -1, List<ItemTrait> damageIgnoreAttributes = null, bool stunNearbyPlayers = false)
		{
			if (nodeType == null)
			{
				nodeType = new ExplosionNode();
			}

			nodeType.IgnoreAttributes = damageIgnoreAttributes;

			int Radius = 0;
			if (fixedRadius <= 0)
			{
				Radius = (int)Math.Round(strength / (Math.PI * 75)) + 5;
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

			if (stunNearbyPlayers)
			{
				StunAndFlashPlayers(WorldPOS.To2Int());
			}


			ScoreMachine.AddToScoreInt(1, RoundEndScoreBuilder.COMMON_SCORE_EXPLOSION);
		}

		private static void StunAndFlashPlayers(Vector2Int startingPos)
		{
			var s = Physics2D.OverlapCircleAll(startingPos, 5, LayerMask.GetMask("Players"));
			Debug.Log(s.Length);
			foreach (Collider2D obj in s)
			{
				var result = MatrixManager.Linecast(
					startingPos.To3Int(), LayerTypeSelection.Walls, null,
					obj.gameObject.AssumedWorldPosServer(), true);
				if (result.ItHit) continue;
				if (obj.gameObject.TryGetComponentCustom<LivingHealthMasterBase>(out var livingHealthMasterBase) == false) continue;
				//TODO Stun , result.Distance <= 3, 9
				//GameObject client, float flashDuration, bool checkForProtectiveCloth, bool stunPlayer = true, float stunDuration = 4f
				livingHealthMasterBase.TryFlash(5, true);
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