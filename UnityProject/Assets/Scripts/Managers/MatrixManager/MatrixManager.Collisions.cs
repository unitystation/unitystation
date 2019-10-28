
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Collision-related stuff
/// </summary>
public partial class MatrixManager
{
	private const float EXTRA_COLLISION_THRESHOLD = 200f;

	/// <summary>
	/// Currently moving matrices. Should be monitored for new intersections with other matrix bounds
	/// </summary>
	private List<MatrixInfo> movingMatrices = new List<MatrixInfo>();

	/// <summary>
	/// bounds intersections that should be actively checked for actual tile collisions
	/// </summary>
	private List<MatrixIntersection> trackedIntersections = new List<MatrixIntersection>();

	public List<MatrixIntersection> TrackedIntersections => trackedIntersections;

	private static LayerType[] layersToRemove = { LayerType.Effects };
	private static LayerType[] effectsToRemove = { LayerType.Effects, LayerType.Grills, LayerType.Objects };
	private static readonly float JUST_DESTROY_THRESHOLD = 250f;

	private void InitCollisions()
	{
		if (!Application.isPlaying || !CustomNetworkManager.Instance._isServer)
		{
			return;
		}

		foreach ( var movableMatrix in MovableMatrices )
		{
			movableMatrix.MatrixMove.OnStart.AddListener( () =>
			{
				if ( !movingMatrices.Contains( movableMatrix ) )
				{
					movingMatrices.Add( movableMatrix );
				}
			} );
			movableMatrix.MatrixMove.OnStop.AddListener( () =>
			{
				if ( movingMatrices.Contains( movableMatrix ) )
				{
					movingMatrices.Remove( movableMatrix );
					trackedIntersections.RemoveAll( intersection => intersection.Matrix1 == movableMatrix );
				}
			} );
		}
	}

	private void RefreshIntersections()
	{
		if ( movingMatrices.Count == 0 )
		{
			if ( trackedIntersections.Count > 0 )
			{
				trackedIntersections.Clear();
			}
			return;
		}

		PruneTracking();
		TrackNewIntersections();

		void PruneTracking()
		{
			List<MatrixIntersection> toRemove = null;
			List<MatrixIntersection> toUpdate = null;

			foreach ( var trackedIntersection in trackedIntersections )
			{
				if ( trackedIntersection.Matrix1.BoundsIntersect( trackedIntersection.Matrix2, out Rect hotZone ) )
				{ //refresh rect
					if ( toUpdate == null )
					{
						toUpdate = new List<MatrixIntersection>();
					}

					toUpdate.Add( new MatrixIntersection
					{
						Matrix1 = trackedIntersection.Matrix1,
						Matrix2 = trackedIntersection.Matrix2,
						Rect = hotZone
					} );
				}
				else
				{ //stop tracking non-intersecting ones
					if ( toRemove == null )
					{
						toRemove = new List<MatrixIntersection>();
					}

					toRemove.Add( trackedIntersection );
				}
			}

			if ( toUpdate != null )
			{
				foreach ( var updateMe in toUpdate )
				{
					trackedIntersections.Remove( updateMe );
					trackedIntersections.Add( updateMe );
				}
			}

			if ( toRemove != null )
			{
				foreach ( var removeMe in toRemove )
				{
					trackedIntersections.Remove( removeMe );
				}
			}

		}

		void TrackNewIntersections()
		{
			foreach ( var movingMatrix in movingMatrices )
			{
				var intersections = GetIntersections( movingMatrix );
				if ( intersections == noIntersections )
				{
					continue;
				}

				foreach ( var intersection in intersections )
				{
					if ( trackedIntersections.Contains( intersection ) )
					{
						continue;
					}

					trackedIntersections.Add( intersection );
				}
			}
		}
	}

	private static readonly MatrixIntersection[] noIntersections = new MatrixIntersection[0];

	private MatrixIntersection[] GetIntersections( MatrixInfo matrix )
	{
		List<MatrixIntersection> intersections = null;
		foreach ( var otherMatrix in ActiveMatrices )
		{
			if ( matrix == otherMatrix )
			{
				continue;
			}
			if ( matrix.BoundsIntersect( otherMatrix, out Rect hotZone ) )
			{
				if ( intersections == null )
				{
					intersections = new List<MatrixIntersection>();
				}

				intersections.Add( new MatrixIntersection
				{
					Matrix1 = matrix,
					Matrix2 = otherMatrix,
					Rect = hotZone
				} );
			}
		}

		if ( intersections != null )
		{
			return intersections.ToArray();
		}
		return noIntersections;
	}

	private void Update()
	{
		if (!Application.isPlaying || !CustomNetworkManager.Instance._isServer)
		{
			return;
		}

		RefreshIntersections();

		if ( trackedIntersections.Count == 0 )
		{
			return;
		}

		for ( var i = trackedIntersections.Count - 1; i >= 0; i-- )
		{
			CheckTileCollisions( trackedIntersections[i] );
		}
	}

	private void CheckTileCollisions( MatrixIntersection i )
	{
		byte collisions = 0;
		foreach ( Vector3Int worldPos in i.Rect.ToBoundsInt().allPositionsWithin )
		{
			Vector3Int cellPos1 = i.Matrix1.MetaTileMap.WorldToCell( worldPos );

			if ( !i.Matrix1.Matrix.HasTile( cellPos1, true) )
			{
				continue;
			}

			Vector3Int cellPos2 = i.Matrix2.MetaTileMap.WorldToCell( worldPos );
			if ( !i.Matrix2.Matrix.HasTile( cellPos2, true) )
			{
				continue;
			}

			float resistance1 = i.Matrix1.MetaTileMap.Resistance( cellPos1, true );
			if ( resistance1 <= 0f )
			{
				continue;
			}

			float resistance2 = i.Matrix2.MetaTileMap.Resistance( cellPos2, true );
			if ( resistance2 <= 0f )
			{
				continue;
			}


			//
			// ******** DESTROY STUFF!!! ********
			//

			//todo: placeholder, must take movement vectors in account! + what hits what
			//total damage to apply to victim tile
			if ( resistance1 + (100 * i.Matrix1.Speed) >= resistance2 + (100 * i.Matrix2.Speed) )
			{
				//attacker tile is stronger:

				//destroy victim tile
				ApplyCritDamage( i.Matrix2, cellPos2, worldPos );

				//slightly damage adjacent victim tiles
				ApplyTilemapDamage( i.Matrix2, cellPos2+Vector3Int.up, resistance1*0.4f, worldPos );
				ApplyTilemapDamage( i.Matrix2, cellPos2+Vector3Int.down, resistance1*0.4f, worldPos );
				ApplyTilemapDamage( i.Matrix2, cellPos2+Vector3Int.left, resistance1*0.4f, worldPos );
				ApplyTilemapDamage( i.Matrix2, cellPos2+Vector3Int.right, resistance1*0.4f, worldPos );

				//damage back attacker
				ApplyDamage( i.Matrix1,	cellPos1, resistance2, worldPos );
				if ( resistance2 > EXTRA_COLLISION_THRESHOLD )
				{
					collisions += (byte)( (resistance2-EXTRA_COLLISION_THRESHOLD) / EXTRA_COLLISION_THRESHOLD );
				}

			} else
			{
				//victim tile is stronger

				//destroy weaker tile
				ApplyCritDamage( i.Matrix1,	cellPos1, worldPos );
				//slightly damage adjacent tiles
				ApplyTilemapDamage( i.Matrix1, cellPos1+Vector3Int.up, resistance2*0.4f, worldPos );
				ApplyTilemapDamage( i.Matrix1, cellPos1+Vector3Int.down, resistance2*0.4f, worldPos );
				ApplyTilemapDamage( i.Matrix1, cellPos1+Vector3Int.left, resistance2*0.4f, worldPos );
				ApplyTilemapDamage( i.Matrix1, cellPos1+Vector3Int.right, resistance2*0.4f, worldPos );

				//damage back
				ApplyDamage( i.Matrix2, cellPos2, resistance1, worldPos );
				if ( resistance1 > EXTRA_COLLISION_THRESHOLD )
				{
					collisions += (byte)( (resistance1-EXTRA_COLLISION_THRESHOLD) / EXTRA_COLLISION_THRESHOLD );
				}
			}

			collisions++;

			//Wires (since they don't have Integrity)
			ApplyWireDamage( i.Matrix1, cellPos1 );
			ApplyWireDamage( i.Matrix2, cellPos2 );

			//Heat shit up
			i.Matrix1.ReactionManager.ExposeHotspot( cellPos1, resistance2, resistance2/1000f );
			i.Matrix2.ReactionManager.ExposeHotspot( cellPos2, resistance1, resistance1/1000f );

			//Other
			foreach ( var layer in layersToRemove )
			{
				i.Matrix1.TileChangeManager.RemoveTile( cellPos1, layer );
				i.Matrix2.TileChangeManager.RemoveTile( cellPos2, layer );
			}
			foreach ( var layer in effectsToRemove )
			{
				i.Matrix1.TileChangeManager.RemoveEffect( cellPos1, layer );
				i.Matrix2.TileChangeManager.RemoveEffect( cellPos2, layer );
			}
		}

		if ( collisions > 0 )
		{
			ExplosionUtils.PlaySoundAndShake(
				i.Rect.position.RoundToInt(),
				(byte) Mathf.Clamp(collisions*5, 5, byte.MaxValue),
				Mathf.Clamp(collisions*5, 15, 80)
				);
			SlowDown( i, collisions );
		}

		//Damage methods
		void ApplyDamage( MatrixInfo victimMatrix, Vector3Int cellPos, float hitEnergy, Vector3Int worldPos )
		{
			//LivingHealthBehaviour
			hitEnergy -= ApplyLivingDamage( victimMatrix, cellPos, hitEnergy );

			//TilemapDamage
			ApplyTilemapDamage( victimMatrix, cellPos, hitEnergy, worldPos );

//			//Integrity
			ApplyIntegrityDamage( victimMatrix, cellPos, hitEnergy );
		}

		void ApplyCritDamage( MatrixInfo victimMatrix, Vector3Int cellPos, Vector3Int worldPos )
		{
			//LivingHealthBehaviour
			ApplyLivingDamage( victimMatrix, cellPos, 9001 );

			//TilemapDamage
			ApplyTilemapDamage( victimMatrix, cellPos, 9001, worldPos);

//			//Integrity
			ApplyIntegrityDamage( victimMatrix, cellPos, 9001 );
		}

		void ApplyTilemapDamage( MatrixInfo matrix, Vector3Int cellPos, float damage, Vector3Int worldPos )
		{
			matrix.MetaTileMap.ApplyDamage( cellPos, damage, worldPos );
			if ( damage > 9000 )
			{
				foreach ( var damageableLayer in matrix.MetaTileMap.DamageableLayers )
				{
					if ( damageableLayer.LayerType == LayerType.Objects )
					{
						continue;
					}
					matrix.TileChangeManager.RemoveTile( cellPos, damageableLayer.LayerType );
				}
			}
		}

		void ApplyWireDamage( MatrixInfo matrix, Vector3Int cellPos )
		{
			foreach ( var wire in matrix.Matrix.Get<CableInheritance>( cellPos, true ) )
			{
				if ( Random.value >= 0.5 )
				{ //Sparks
					wire.QueueForDemolition( wire );
				} else
				{ //Destruction
					wire.toDestroy();
				}
			}
		}

		float ApplyIntegrityDamage( MatrixInfo matrix, Vector3Int cellPos, float damage )
		{
			float resistance = 0f;
			foreach ( var integrity in matrix.Matrix.Get<Integrity>( cellPos, true ) )
			{
				resistance += integrity.integrity;
				integrity.ApplyDamage( damage, AttackType.Melee, DamageType.Brute );
			}

			return resistance;
		}

		float ApplyLivingDamage( MatrixInfo matrix, Vector3Int cellPos, float damage )
		{
			byte count = 0;
			foreach ( var healthBehaviour in matrix.Matrix.Get<LivingHealthBehaviour>( cellPos, true ) )
			{
				healthBehaviour.ApplyDamageToBodypart( null, damage, AttackType.Melee, DamageType.Brute );
				count++;
			}

			return count * 50;
		}
	}



	private void SlowDown( MatrixIntersection i, int collisions )
	{
		if ( i.Matrix1.IsMovable && i.Matrix1.MatrixMove.isMovingServer )
		{
			InternalSlowDown( i.Matrix1 );
		}
		if ( i.Matrix2.IsMovable && i.Matrix2.MatrixMove.isMovingServer )
		{
			InternalSlowDown( i.Matrix2 );
		}

		void InternalSlowDown( MatrixInfo info )
		{
			float slowdownFactor = Mathf.Clamp(
				1f - ( Mathf.Clamp( collisions, 1, 50 ) / 100f ) + info.Mass,
				0.1f,
				0.95f
				);
			float speed = ( info.MatrixMove.State.Speed * slowdownFactor ) - 0.07f;
			if ( speed <= 1f )
			{
				info.MatrixMove.StopMovement();
			} else
			{
				info.MatrixMove.SetSpeed( speed );
			}
		}
	}

	private void OnDrawGizmos()
	{
		if (!Application.isPlaying || !IsInitialized) return;
		foreach ( var intersection in Instance.TrackedIntersections )
		{
			Gizmos.color = Color.red;
			DebugGizmoUtils.DrawRect( intersection.Matrix1.WorldBounds );
			Gizmos.color = Color.blue;
			DebugGizmoUtils.DrawRect( intersection.Matrix2.WorldBounds );
			Gizmos.color = Color.yellow;
			DebugGizmoUtils.DrawRect( intersection.Rect );
		}
	}
}

/// <summary>
/// First and second matrix are swappable – intersections (m1,m2) and (m2,m1) will be considered equal.
/// Rect isn't checked for equality
/// </summary>
public struct MatrixIntersection
{
	public MatrixInfo Matrix1;
	public MatrixInfo Matrix2;
	public Rect Rect;

	public override int GetHashCode()
	{
		return Matrix1.GetHashCode() ^ Matrix2.GetHashCode();
	}

	public bool Equals( MatrixIntersection other )
	{
		return (Matrix1.Equals( other.Matrix1 ) && Matrix2.Equals( other.Matrix2 ))
		       || (Matrix1.Equals( other.Matrix2 ) && Matrix2.Equals( other.Matrix1 ));	}

	public override bool Equals( object obj )
	{
		return obj is MatrixIntersection other && Equals( other );
	}

	public static bool operator ==( MatrixIntersection left, MatrixIntersection right )
	{
		return left.Equals( right );
	}

	public static bool operator !=( MatrixIntersection left, MatrixIntersection right )
	{
		return !left.Equals( right );
	}
}