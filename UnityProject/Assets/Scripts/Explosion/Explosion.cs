using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
	public EffectShapeType explosionType;
	[TooltipAttribute("Distance multiplied from explosion that will still shake = shakeDistance * radius")]
	public float shakeDistance = 8;
	[TooltipAttribute("generally necessary for smaller explosions = 1 - ((distance + distance) / ((radius + radius) + minDamage))")]
	public int minDamage = 2;
	[TooltipAttribute("Maximum duration grenade effects are visible depending on distance from center")]
	public float maxEffectDuration = .25f;
	[TooltipAttribute("Minimum duration grenade effects are visible depending on distance from center")]
	public float minEffectDuration = .05f;

	private LayerMask obstacleMask;

	/// <summary>
	/// Create explosion on selected matrix
	/// </summary>
	/// <param name="matrix"></param>
	public void Explode(Matrix matrix)
	{
		obstacleMask = LayerMask.GetMask("Walls", "Door Closed");
		StartCoroutine(ExplosionRoutine(matrix));
	}

	public void SetExplosionData(int damage = 150, float radius = 4f, bool unstableRadius = false, EffectShapeType explosionType = EffectShapeType.Circle, float shakeDistance = 8, int minDamage = 2, float maxEffectDuration = .25f, float minEffectDuration = .05f)
	{
		this.damage = damage;
		this.radius = radius;
		this.unstableRadius = unstableRadius;
		this.explosionType = explosionType;
		this.shakeDistance = shakeDistance;
		this.minDamage = minDamage;
		this.maxEffectDuration = maxEffectDuration;
		this.minEffectDuration = minEffectDuration;
	}

	private IEnumerator ExplosionRoutine(Matrix matrix)
	{
		var explosionCenter = transform.position.RoundToInt();

		// First - play boom sound and shake ground
		PlaySoundAndShake(explosionCenter);

		// Now let's create explosion shape
		int radiusInteger = (int)radius;
		var shape = EffectShape.CreateEffectShape(explosionType, explosionCenter, radiusInteger);

		var explosionCenter2d = explosionCenter.To2Int();
		var tileManager = GetComponentInParent<TileChangeManager>();
		var longestTime = 0f;

		foreach (var tilePos in shape)
		{
			float distance = Vector3Int.Distance(tilePos, explosionCenter);
			var tilePos2d = tilePos.To2Int();

			// Is explosion goes behind walls?
			if (IsPastWall(explosionCenter2d, tilePos2d, distance))
			{
				// Heat the air
				matrix.ReactionManager.ExposeHotspotWorldPosition(tilePos2d, 3200, 0.005f);

				// Calculate damage from explosion
				int damage = CalculateDamage(tilePos2d, explosionCenter2d);

				if (damage > 0)
				{
					// Damage poor living things
					DamageLivingThings(tilePos, damage);

					// Damage all objects
					DamageObjects(tilePos, damage);

					// Damage all tiles
					DamageTiles(tilePos, damage);
				}

				// Calculate fire effect time
				var fireTime = DistanceFromCenter(explosionCenter2d, tilePos2d, minEffectDuration, maxEffectDuration);

				if (float.IsNaN(fireTime))
				{
					fireTime = 0f;
				}

				var localTilePos = MatrixManager.WorldToLocalInt(tilePos, matrix.Id);
				StartCoroutine(TimedFireEffect(localTilePos, fireTime, tileManager));

				// Save longest fire effect time
				if (fireTime > longestTime)
					longestTime = fireTime;
			}
		}

		// Wait until all fire effects are finished
		yield return WaitFor.Seconds(longestTime);

		Destroy(gameObject);
	}

	public IEnumerator TimedFireEffect(Vector3Int position, float time, TileChangeManager tileChangeManager)
	{
		// Store the old effect for restoring after fire is gone
		LayerTile oldEffectLayerTile = tileChangeManager.GetLayerTile(position, LayerType.Effects);

		tileChangeManager.UpdateTile(position, TileType.Effects, "Fire");
		yield return WaitFor.Seconds(time);
		tileChangeManager.RemoveTile(position, LayerType.Effects);

		// Restore the old effect if any (ex: cracked glass)
		if (oldEffectLayerTile)
			tileChangeManager.UpdateTile(position, oldEffectLayerTile);
	}



	private void DamageLivingThings(Vector3Int worldPosition, int damage)
	{
		var damagedLivingThings = (MatrixManager.GetAt<LivingHealthBehaviour>(worldPosition, true)
			//only damage each thing once
			.Distinct());

		foreach (var damagedLiving in damagedLivingThings)
		{
			damagedLiving.ApplyDamage(gameObject, damage, AttackType.Bomb, DamageType.Burn);
		}
	}

	private void DamageObjects(Vector3Int worldPosition, int damage)
	{
		var damagedObjects = (MatrixManager.GetAt<Integrity>(worldPosition, true)
			//only damage each thing once
			.Distinct());

		foreach (var damagedObject in damagedObjects)
        {
	        damagedObject.ApplyDamage(damage, AttackType.Bomb, DamageType.Burn);
        }
	}

	private void DamageTiles(Vector3Int worldPosition, int damage)
	{
		var matrix = MatrixManager.AtPoint(worldPosition, true);
		matrix.MetaTileMap.ApplyDamage(MatrixManager.WorldToLocalInt(worldPosition, matrix), damage, worldPosition, AttackType.Bomb);
	}

	/// <summary>
	/// Plays explosion sound and shakes ground
	/// </summary>
	private void PlaySoundAndShake(Vector3Int explosionPosition)
	{
		byte shakeIntensity = (byte)Mathf.Clamp( damage/5, byte.MinValue, byte.MaxValue);
		ExplosionUtils.PlaySoundAndShake(explosionPosition, shakeIntensity, (int) shakeDistance);
	}

	private bool IsPastWall(Vector2Int pos, Vector2Int damageablePos, float distance)
	{
		return Physics2D.Raycast(pos, damageablePos - pos, distance, obstacleMask).collider == null;
	}

	private int CalculateDamage(Vector2Int damagePos, Vector2Int explosionPos)
	{
		float distance = Vector2Int.Distance(explosionPos, damagePos);
		float effect = 1 - ((distance + distance) / ((radius + radius) + minDamage));
		return (int)(damage * effect);
	}

	/// <summary>
	/// calculates the distance from the the center using the looping x and y vars
	/// returns a float between the limits
	/// </summary>
	private float DistanceFromCenter(Vector2Int pos, Vector2Int center, float lowLimit = 0.05f, float highLimit = 0.25f)
	{
		var dif = center - pos;

		float percentage = (Mathf.Abs(dif.x) + Mathf.Abs(dif.y)) / (radius + radius);
		float reversedPercentage = (1 - percentage) * 100;
		float distance = ((reversedPercentage * (highLimit - lowLimit) / 100) + lowLimit);
		return distance;
	}
}
