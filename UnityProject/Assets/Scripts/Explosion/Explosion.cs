using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Explosion : MonoBehaviour
{
	[TooltipAttribute("If explosion radius has a degree of error equal to radius / 4")]
	public bool unstableRadius = false;
	[TooltipAttribute("Explosion Damage")]
	public int damage = 150;
	[TooltipAttribute("Explosion Radius in tiles")]
	public float radius = 4f;
	[TooltipAttribute("Shape of the explosion")]
	public ExplosionType explosionType;
	[TooltipAttribute("Distance multiplied from explosion that will still shake = shakeDistance * radius")]
	public float shakeDistance = 8;
	[TooltipAttribute("generally necessary for smaller explosions = 1 - ((distance + distance) / ((radius + radius) + minDamage))")]
	public int minDamage = 2;
	[TooltipAttribute("Maximum duration grenade effects are visible depending on distance from center")]
	public float maxEffectDuration = .25f;
	[TooltipAttribute("Minimum duration grenade effects are visible depending on distance from center")]
	public float minEffectDuration = .05f;

	public void Explode()
	{
		StartCoroutine(ExplosionRoutine());
	}

	/// <summary>
	/// calculates the distance from the the center using the looping x and y vars
	/// returns a float between the limits
	/// </summary>
	private float DistanceFromCenter(int x, int y, float lowLimit = 0.05f, float highLimit = 0.25f)
	{
		float percentage = (Mathf.Abs(x) + Mathf.Abs(y)) / (radius + radius);
		float reversedPercentage = (1 - percentage) * 100;
		float distance = ((reversedPercentage * (highLimit - lowLimit) / 100) + lowLimit);
		return distance;
	}

	private IEnumerator ExplosionRoutine()
	{
		var currentPosition = transform.position.RoundToInt();

		// First - play boom sound and shake ground
		PlaySoundAndShake(currentPosition);

		// Now let's create shape
		var shape = CreateShape(currentPosition);
		fore
	}

	/// <summary>
	/// Plays explosion sound and shakes ground
	/// </summary>
	private void PlaySoundAndShake(Vector3Int explosionPosition)
	{
		byte shakeIntensity = (byte)Mathf.Clamp( damage/5, byte.MinValue, byte.MaxValue);
		ExplosionUtils.PlaySoundAndShake(explosionPosition, shakeIntensity, (int) shakeDistance);
	}

	/// <summary>
	/// Set the tiles to show fire effect in the pattern that was chosen
	/// This could be used in the future to set it as chemical reactions in a location instead.
	/// </summary>
	private List<Vector3Int> CreateShape(Vector3Int pos)
	{
		int radiusInteger = (int)radius;

		List<Vector3Int> shape = null;

		switch (explosionType)
		{
			case ExplosionType.Square:
				shape = ExplosionUtils.CreateSquareShapeMargin(pos, radiusInteger);
				break;
		}

		/*if (explosionType == ExplosionType.Square)
		{
				if (IsPastWall(pos.To2Int(), checkPos.To2Int(), Mathf.Abs(i) + Mathf.Abs(j)))
				{
					CheckDamagedThings(checkPos.To2Int());
					checkPos.x -= 1;
					checkPos.y -= 1;
					StartCoroutine(TimedEffect(checkPos, TileType.Effects, "Fire", DistanceFromCenter(i,j, minEffectDuration, maxEffectDuration)));
				}

		}
		if (explosionType == ExplosionType.Diamond)
		{
			// F is distance from zero, calculated by radius - x
			// if pos.x/pos.y is within that range it will apply affect that position
			int f;
			for (int i = -radiusInteger; i <= radiusInteger; i++)
			{
				f = radiusInteger - Mathf.Abs(i);
				for (int j = -radiusInteger; j <= radiusInteger; j++)
				{
					if (j <= 0 && j >= (-f) || j >= 0 && j <= (0 + f))
					{
						Vector3Int diamondPos = new Vector3Int(pos.x + i, pos.y + j, 0);
						if (IsPastWall(pos.To2Int(), diamondPos.To2Int(), Mathf.Abs(i) + Mathf.Abs(j)))
						{
							CheckDamagedThings(diamondPos.To2Int());
							diamondPos.x -= 1;
							diamondPos.y -= 1;
							StartCoroutine(TimedEffect(diamondPos, TileType.Effects, "Fire", DistanceFromCenter(i,j, minEffectDuration, maxEffectDuration)));
						}
					}
				}
			}
		}
		if (explosionType == ExplosionType.Bomberman)
		{
			for (int i = -radiusInteger; i <= radiusInteger; i++)
			{
				Vector3Int xPos = new Vector3Int(pos.x + i, pos.y, 0);
				if (IsPastWall(pos.To2Int(), xPos.To2Int(), Mathf.Abs(i)))
				{
					CheckDamagedThings(xPos.To2Int());
					xPos.x -= 1;
					xPos.y -= 1;
					StartCoroutine(TimedEffect(xPos, TileType.Effects, "Fire", DistanceFromCenter(i,0, minEffectDuration, maxEffectDuration)));
				}
			}
			for (int j = -radiusInteger; j <= radiusInteger; j++)
			{
				Vector3Int yPos = new Vector3Int(pos.x, pos.y + j, 0);
				if (IsPastWall(pos.To2Int(), yPos.To2Int(), Mathf.Abs(j)))
				{
					CheckDamagedThings(yPos.To2Int());
					yPos.x -= 1;
					yPos.y -= 1;
					StartCoroutine(TimedEffect(yPos, TileType.Effects, "Fire", DistanceFromCenter(0,j, minEffectDuration, maxEffectDuration)));
				}
			}
		}
		if (explosionType == ExplosionType.Circle)
		{
			// F is distance from zero, calculated by radius - x
			// if pos.x/pos.y is within that range it will apply affect that position
			int f;
			for (int i = -radiusInteger; i <= radiusInteger; i++)
			{
				f = radiusInteger - Mathf.Abs(i) + 1;
				for (int j = -radiusInteger; j <= radiusInteger; j++)
				{
					if (j <= 0 && j >= (-f) || j >= 0 && j <= (0 + f))
					{
						Vector3Int circlePos = new Vector3Int(pos.x + i, pos.y + j, 0);
						if (IsPastWall(pos.To2Int(), circlePos.To2Int(), Mathf.Abs(i) + Mathf.Abs(j)))
						{
							CheckDamagedThings(circlePos.To2Int());
							circlePos.x -= 1;
							circlePos.y -= 1;
							StartCoroutine(TimedEffect(circlePos, TileType.Effects, "Fire", DistanceFromCenter(i,j, minEffectDuration, maxEffectDuration)));
						}
					}
				}
			}
		}*/

		return shape;
	}

	private bool IsPastWall(Vector2 pos, Vector2 damageablePos, float distance)
	{
		return Physics2D.Raycast(pos, damageablePos - pos, distance, OBSTACLE_MASK).collider == null;
	}
}
