
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
	private List<Tuple<MatrixInfo,MatrixInfo>> trackedIntersections = new List<Tuple<MatrixInfo,MatrixInfo>>();

	public List<Tuple<MatrixInfo, MatrixInfo>> TrackedIntersections => trackedIntersections;

	private void InitCollisions()
	{
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
					trackedIntersections.RemoveAll( intersection => intersection.Item1 == movableMatrix );
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
			List<Tuple<MatrixInfo, MatrixInfo>> stopTracking = null;
			foreach ( var trackedIntersection in trackedIntersections )
			{
				if ( !trackedIntersection.Item1.BoundsIntersect( trackedIntersection.Item2 ) )
				{
					if ( stopTracking == null )
					{
						stopTracking = new List<Tuple<MatrixInfo, MatrixInfo>>();
					}

					stopTracking.Add( trackedIntersection );
				}
			}

			if ( stopTracking != null )
			{
				foreach ( var toRemove in stopTracking )
				{
					trackedIntersections.Remove( toRemove );
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

				foreach ( var intersectingMatrix in intersections )
				{
					var intersection = new Tuple<MatrixInfo, MatrixInfo>( movingMatrix, intersectingMatrix );
					//todo: figure out situation with 2 shuttles ramming each other
					//		because then there will be 2 mirrored intersections - (one, two) and (two, one)
					if ( trackedIntersections.Contains( intersection ) )
					{
						continue;
					}

					trackedIntersections.Add( intersection );
				}
			}
		}
	}

	private static readonly MatrixInfo[] noIntersections = new MatrixInfo[0];

	private MatrixInfo[] GetIntersections( MatrixInfo matrix )
	{
		List<MatrixInfo> intersections = null;
		foreach ( var otherMatrix in ActiveMatrices )
		{
			if ( matrix == otherMatrix )
			{
				continue;
			}
			if ( matrix.BoundsIntersect( otherMatrix ) )
			{
				if ( intersections == null )
				{
					intersections = new List<MatrixInfo>();
				}

				intersections.Add( otherMatrix );
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

		RefreshIntersections();

		if ( trackedIntersections.Count == 0 )
		{
			return;
		}

		foreach ( var trackedIntersection in trackedIntersections )
		{
			CheckCollisions( trackedIntersection.Item1, trackedIntersection.Item2 );
		}
	}

	private void CheckCollisions( MatrixInfo firstMatrix, MatrixInfo secondMatrix )
	{
		//todo: calculate intersection rect and check tiles inside it
	}

	private void OnDrawGizmos()
	{
		if (!Application.isPlaying || !IsInitialized) return;
		foreach ( var intersection in Instance.TrackedIntersections )
		{
			Gizmos.color = Color.red;
			DebugGizmoUtils.DrawRect( intersection.Item1.WorldBounds );
			Gizmos.color = Color.blue;
			DebugGizmoUtils.DrawRect( intersection.Item2.WorldBounds );

		}
	}
}
