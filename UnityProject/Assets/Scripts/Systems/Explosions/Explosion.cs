using System;
using System.Collections.Generic;
using Core;
using HealthV2;
using Logs;
using Systems.Score;
using UnityEngine;

namespace Systems.Explosions
{
	public class Explosion
	{

		// (Max) - why were these numbers choosen before?
		// They may look less like magic numbers now, but there is no explanation for why they multiples of 8.
		public const int EXPLOSION_STRENGTH_LOW = 800;
		public const int EXPLOSION_STRENGTH_MEDIUM = 8000;
		public const int EXPLOSION_STRENGTH_HIGH = 80000;

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
				if (strength > EXPLOSION_STRENGTH_LOW)
				{
					ShakingStrength = 75;
				}
				else if (strength > EXPLOSION_STRENGTH_MEDIUM)
				{
					ShakingStrength = 125;
				}
				else if (strength > EXPLOSION_STRENGTH_HIGH)
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

			// we assume that the explosion isn't something small like an EMP gernade or
			if (stunNearbyPlayers || strength > EXPLOSION_STRENGTH_HIGH)
			{
				StunAndFlashPlayers(WorldPOS.To2Int(), strength);
			}

			ScoreMachine.AddToScoreInt(1, RoundEndScoreBuilder.COMMON_SCORE_EXPLOSION);
		}

		public static void StunAndFlashPlayers(Vector2Int startingPos, float strength)
		{
			var distance = GetDistanceFromStrength(strength);;
			var s = ComponentsTracker<LivingHealthMasterBase>.GetAllNearbyTypesToLocation(startingPos.To3(), distance);
			foreach (var obj in s)
			{
				if (distance < 23)
				{
					if (IsStunReachable(startingPos, obj) == false) continue;
				}
				obj.TryFlash(5, true);
			}
		}

		private static bool IsStunReachable(Vector2Int startingPos, LivingHealthMasterBase obj)
		{
			var result = MatrixManager.Linecast(
				startingPos.To3Int(), LayerTypeSelection.Walls, null,
				obj.gameObject.AssumedWorldPosServer(), true);
			if (result.ItHit)
			{
#if UNITY_EDITOR
				Loggy.Log($"[Explosion/StunAndFlashPlayers()] - " +
				          $"We hit {result.CollisionHit.GameObject?.ExpensiveName()} when using MatrixManger.Linecraft().", Category.TileMaps);
#endif
				return false;
			}
			return true;
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

		private static int GetDistanceFromStrength(float strength)
		{
			return strength switch
			{
				< 100 => 2, // fairly smol explosion
				< 400 => 3,
				< 800 => 6,
				< 4000 => 8,
				< 12000 => 12, // gernade
				< 22000 => 14,
				< 62000 => 16,
				< 80000 => 24, // why do I hear loud beeping?
				< 120000 => 48,
				< 166000 => 64, // powersink with high energy siphon
				< 248000 => 128, // powersink with maxcap bypass
				< 648000 => 256, // someone is trying to be funny or is trying to crash the server
				< 1000000 => 512, // nuke
				_ => 6
			};
		}
	}
}