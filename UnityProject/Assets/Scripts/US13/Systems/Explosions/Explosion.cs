using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Logs;
using UnityEngine;
using US13.Core;
using US13.Core.Admin.Logs;
using US13.HealthV2.Living;
using US13.Items.Traits;
using US13.Items.Weapons;
using US13.Managers.MatrixManager;
using US13.Systems.Explosions.NodeTypes;
using US13.Systems.Score;
using US13.Tilemaps.Utils;
using Util;

namespace US13.Systems.Explosions
{
	public class Explosion
	{

		// (Max) - why were these numbers choosen before?
		// They may look less like magic numbers now, but there is no explanation for why they are multiples of 8.
		public const int EXPLOSION_STRENGTH_LOW = 800;
		public const int EXPLOSION_STRENGTH_MEDIUM = 8000;
		public const int EXPLOSION_STRENGTH_HIGH = 80000;
		public const int NUKE_FLASH_DISTANCE = 12580;

		public class ExplosionData
		{
			public HashSet<Vector2Int> CircleCircumference = new HashSet<Vector2Int>();
		}

		public static void StartExplosion(Vector3Int WorldPOS, float strength, ExplosionNode nodeType = null,
			int fixedRadius = -1, int fixedShakingStrength = -1, List<ItemTrait> damageIgnoreAttributes = null, bool stunNearbyPlayers = false, int radiusMultiplier = 1)
		{
			AdminLogsManager.AddNewLog(null, $"An explosion has occured at {WorldPOS} with strength: {strength}.", LogCategory.World,
				Severity.SUSPICOUS);
			nodeType ??= new ExplosionNode(WorldPOS);
			nodeType.IgnoreAttributes = damageIgnoreAttributes;

			int radius = 0;
			float strengthMag = Math.Abs(strength);
			if (fixedRadius <= 0)
			{
				radius = (int)(Math.Round(strength / (Math.PI * 75)) + 5) * radiusMultiplier;
			}
			else
			{
				radius = fixedRadius;
			}
			if (radius > 150)
			{
				radius = 150;
			}

			byte shakingStrength = 0;
			if (fixedShakingStrength <= 0 || fixedShakingStrength > 255)
			{
				shakingStrength = 25;
				if (strengthMag > EXPLOSION_STRENGTH_LOW)
				{
					shakingStrength = 75;
				}
				else if (strengthMag > EXPLOSION_STRENGTH_MEDIUM)
				{
					shakingStrength = 125;
				}
				else if (strengthMag > EXPLOSION_STRENGTH_HIGH)
				{
					shakingStrength = 255;
				}
			}
			else
			{
				shakingStrength = (byte)fixedShakingStrength;
			}

			float volumeMultiplier = Mathf.Clamp(strengthMag / EXPLOSION_STRENGTH_LOW, 0.25f, 1);
			ExplosionUtils.PlaySoundAndShake(WorldPOS, shakingStrength, radius / 20, nodeType.CustomSound, volumeMultiplier);

			//Generates the conference
			var explosionData = new ExplosionData();
			circleBres(explosionData, WorldPOS.x, WorldPOS.y, radius);

			float initialStrength = strength / explosionData.CircleCircumference.Count;

			foreach (var toPoint in explosionData.CircleCircumference)
			{
				var line = ExplosionPropagationLine.Getline();
				line.SetUp(WorldPOS.x, WorldPOS.y, toPoint.x, toPoint.y, initialStrength, nodeType);
				line.Step();
			}

			// we assume that the explosion isn't something small like an EMP gernade or
			if (stunNearbyPlayers || strengthMag > EXPLOSION_STRENGTH_HIGH)
			{
				_ = StunAndFlashPlayers(WorldPOS.To2Int(), strengthMag);
			}

			ScoreMachine.AddToScoreInt(1, RoundEndScoreBuilder.COMMON_SCORE_EXPLOSION);
		}

		public static async UniTask StunAndFlashPlayers(Vector2Int startingPos, float strength)
		{
			var distance = GetDistanceFromStrength(strength);;
			var s = ComponentsTracker<LivingHealthMasterBase>.GetAllNearbyTypesToLocation(startingPos.To3(), distance);
			foreach (var obj in s)
			{
				await UniTask.Delay(25);
				// for performance reasons, if we have a big enough explosion: skip physics line checks as they're expensive.
				// large explosions are slow enough as is because it has to damage/check hundreds of objects which all trigger
				// different behaviors and events. We shouldn't strain the server with extra physics check ontop of that.
				if (distance < 12)
				{
					if (IsStunReachable(startingPos, obj) == false) continue;
				}
				// if the explosion is too strong, skip flash protection check.
				obj.TryFlash(5, strength < EXPLOSION_STRENGTH_HIGH);
			}
		}

		private static bool IsStunReachable(Vector2Int startingPos, LivingHealthMasterBase obj)
		{
			var result = MatrixManager.Linecast(
				startingPos.To3Int(), LayerTypeSelection.Walls, null,
				obj.gameObject.AssumedWorldPosServer(), DEBUG:true);
			if (result.ItHit)
			{
#if UNITY_EDITOR
				Loggy.Info($"[Explosion/StunAndFlashPlayers()] - " +
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
			if (strength < 92000)
			{
				return (int)Math.Ceiling(Math.Log(strength / 100.0) * 2);
			}
			return NUKE_FLASH_DISTANCE;
		}
	}
}