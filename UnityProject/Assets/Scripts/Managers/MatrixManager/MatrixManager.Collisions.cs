
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Collision-related stuff
/// </summary>
public partial class MatrixManager
{
	/// <summary>
	/// Currently moving matrices. Should be monitored for new intersections with other matrix bounds
	/// </summary>
	private List<MatrixInfo> movingMatrices = new List<MatrixInfo>();

	/// <summary>
	/// bounds intersections that should be actively checked for actual tile collisions
	/// </summary>
	private List<MatrixIntersection> trackedIntersections = new List<MatrixIntersection>();

	public List<MatrixIntersection> TrackedIntersections => trackedIntersections;

	private void InitCollisions()
	{
		if (!CustomNetworkManager.Instance._isServer)
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
		if (!CustomNetworkManager.Instance._isServer)
		{
			return;
		}

		RefreshIntersections();

		if ( trackedIntersections.Count == 0 )
		{
			return;
		}

		foreach ( var trackedIntersection in trackedIntersections )
		{
			CheckTileCollisions( trackedIntersection );
		}
	}

	private void CheckTileCollisions( MatrixIntersection i )
	{
//		List<Vector3Int> nonEmpty = null;
		Vector3Int cellPos1;
		Vector3Int cellPos2;
		int collisions = 0;
		foreach ( Vector3Int worldPos in i.Rect.ToBoundsInt().allPositionsWithin )
		{
			cellPos1 = i.Matrix1.MetaTileMap.WorldToCell( worldPos );
			if ( i.Matrix1.Matrix.IsEmptyAt( cellPos1, true ) )
			{
				continue;
			}

			cellPos2 = i.Matrix2.MetaTileMap.WorldToCell( worldPos );
			if ( i.Matrix2.Matrix.IsEmptyAt( cellPos2, true ) )
			{
				continue;
			}

			i.Matrix1.TileChangeManager.RemoveTile( cellPos1 );
			i.Matrix2.TileChangeManager.RemoveTile( cellPos2 );
			collisions++;
		}

		if ( collisions > 0 )
		{
			SlowDown( i, collisions );
		}
	}

	//todo: consider mass, wall hardness, speed. reduce speed depending on destroyed tile amount and mass
	private void SlowDown( MatrixIntersection i, int collisions )
	{
		if ( i.Matrix1.IsMovable && i.Matrix1.MatrixMove.isMovingServer )
		{
			InternalSlowDown( i.Matrix1.MatrixMove );
		}
		if ( i.Matrix2.IsMovable && i.Matrix2.MatrixMove.isMovingServer )
		{
			InternalSlowDown( i.Matrix2.MatrixMove );
		}

		void InternalSlowDown( MatrixMove matrixMove )
		{
			float slowdownFactor = 1f - ( Mathf.Clamp( collisions, 1, 50 ) / 100f );
			matrixMove.SetSpeed( ( matrixMove.State.Speed * slowdownFactor ) - 0.1f );
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