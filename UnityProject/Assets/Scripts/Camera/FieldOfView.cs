using System;
using System.Collections;
using System.Collections.Generic;
using PlayGroup;
using UnityEngine;

public class FieldOfView : MonoBehaviour
{
	public float edgeDistanceThreshhold;
	public int edgeResolveIterations;

	public List<Line> lines = new List<Line>(256);

	public float maskCutawayDst;

	public float meshResolution;
	public LayerMask obstacleMask;

	public LayerMask targetMask;
	[Range(0, 360)] public float viewAngle;
	public Mesh viewMesh;

	public MeshFilter viewMeshFilter;
	public float viewRadius;

	//used to show gameobjects the player can see on the target mask
	public List<Transform> visibleTargets = new List<Transform>();

	private void Start()
	{
		viewMesh = new Mesh();
		viewMesh.name = "View Mesh";
		viewMeshFilter.mesh = viewMesh;

		StartCoroutine("FindTargetsWithDelay", 0.05f);
	}

	private void LateUpdate()
	{
		if (!PlayerManager.LocalPlayer)
		{
			return;
		}

		DrawFieldOfView();
	}

	private IEnumerator FindTargetsWithDelay(float delay)
	{
		while (true)
		{
			yield return new WaitForSeconds(delay);
			lines.Clear();
			FindVisibleTargets();
		}
	}

	private void OnDrawGizmos()
	{
		if (lines.Count > 0)
		{
			for (int i = 0; i < lines.Count; i++)
			{
				Line line = lines[i];
				Gizmos.color = Color.red;
				Gizmos.DrawLine(line.start, line.end);
			}
		}
	}

	private void FindVisibleTargets()
	{
		visibleTargets.Clear();
		Vector3 fovPos = transform.position;
		Collider2D[] targetsInViewRadius = Physics2D.OverlapCircleAll(fovPos, viewRadius, targetMask);

		for (int i = 0; i < targetsInViewRadius.Length; i++)
		{
			Transform target = targetsInViewRadius[i].transform;
			Vector3 targetPos = target.position;

			SpriteRenderer[] hideAll = target.gameObject.GetComponentsInChildren<SpriteRenderer>();
			foreach (SpriteRenderer renderer in hideAll)
			{
				renderer.enabled = false;
			}

			Vector3 dirToTarget = (targetPos - fovPos).normalized;

			float x = (float) Math.Round(dirToTarget.x, MidpointRounding.AwayFromZero) / 2;
			float y = (float) Math.Round(dirToTarget.y, MidpointRounding.AwayFromZero) / 2;
			float z = dirToTarget.z;

			if (fovPos.y > targetPos.y)
			{
				y = -0.6f;
			}
			else if (fovPos.y < targetPos.y)
			{
				y = 0.6f;
			}

			if (fovPos.x > targetPos.x)
			{
				x = -0.6f;
			}
			else if (fovPos.x < targetPos.x)
			{
				x = 0.6f;
			}
			else if (fovPos.x == targetPos.x)
			{
				x = 0.0f;
			}

			Vector3 snappedDirToTarget = new Vector3(x, y, z);

			Vector3 normalizedDirection = targetPos + -snappedDirToTarget;

			lines.Add(new Line(fovPos, targetPos));
			//raycast to a position infront of blocks normalized to the direction of the player
			RaycastHit2D rayTest = Physics2D.Linecast(fovPos, normalizedDirection, obstacleMask);
			//lines.Add (new Line (fovPos, normalizedDirection));
			if (rayTest != null && rayTest.collider == null)
			{
				SpriteRenderer[] visibleRenderers =
					target.gameObject.transform.GetComponentsInChildren<SpriteRenderer>();
				foreach (SpriteRenderer renderer in visibleRenderers)
				{
					renderer.enabled = true;
				}
			}
		}
	}

	private void DrawFieldOfView()
	{
		int stepCount = Mathf.RoundToInt(viewAngle * meshResolution);
		float stepAngleSize = viewAngle / stepCount;
		List<Vector3> viewPoints = new List<Vector3>(1024);

		ViewCastInfo oldViewCast = new ViewCastInfo();
		float eulerZ = transform.eulerAngles.z;

		for (int i = 0; i <= stepCount; i++)
		{
			float angle = eulerZ - viewAngle / 2 + stepAngleSize * i;
			ViewCastInfo newViewCast = ViewCast(angle);

			if (i > 0)
			{
				bool edgeDstThreshholdExceeded = Mathf.Abs(oldViewCast.dst - newViewCast.dst) > edgeDistanceThreshhold;
				if (oldViewCast.hit != newViewCast.hit ||
				    oldViewCast.hit && newViewCast.hit && edgeDstThreshholdExceeded)
				{
					EdgeInfo edge = FindEdge(oldViewCast, newViewCast);
					if (edge.pointA != Vector3.zero)
					{
						viewPoints.Add(edge.pointA);
					}
					if (edge.pointB != Vector3.zero)
					{
						viewPoints.Add(edge.pointB);
					}
				}
			}

			viewPoints.Add(newViewCast.point);
			oldViewCast = newViewCast;
		}

		int vertexCount = viewPoints.Count + 1;
		Vector3[] verticies = new Vector3[vertexCount];
		int[] triangles = new int[(vertexCount - 2) * 3];

		verticies[0] = Vector3.zero;

		for (int i = 0; i < vertexCount - 1; i++)
		{
			verticies[i + 1] = transform.InverseTransformPoint(viewPoints[i]);

			if (i < vertexCount - 2)
			{
				triangles[i * 3] = 0;
				triangles[i * 3 + 1] = i + 1;
				triangles[i * 3 + 2] = i + 2;
			}
		}

		viewMesh.Clear();
		viewMesh.vertices = verticies;
		viewMesh.triangles = triangles;
		viewMesh.RecalculateNormals();
	}

	private EdgeInfo FindEdge(ViewCastInfo minViewCast, ViewCastInfo maxViewCast)
	{
		float minAngle = minViewCast.angle;
		float maxAngle = maxViewCast.angle;
		Vector3 minPoint = Vector3.zero;
		Vector3 maxPoint = Vector3.zero;

		for (int i = 0; i < edgeResolveIterations; i++)
		{
			float angle = (minAngle + maxAngle) / 2;
			ViewCastInfo newViewCast = ViewCast(angle);

			bool edgeDstThreshholdExceeded = Mathf.Abs(minViewCast.dst - newViewCast.dst) > edgeDistanceThreshhold;
			if (newViewCast.hit == minViewCast.hit && !edgeDstThreshholdExceeded)
			{
				minAngle = angle;
				minPoint = newViewCast.point;
			}
			else
			{
				maxAngle = angle;
				maxPoint = newViewCast.point;
			}
		}

		return new EdgeInfo(minPoint, maxPoint);
	}

	private ViewCastInfo ViewCast(float globalAngle)
	{
		Vector3 dir = DirFromAngle(globalAngle, true);
		Vector3 fovPos = transform.position;
		RaycastHit2D hit = Physics2D.Raycast(fovPos, dir, viewRadius, obstacleMask);
		if (hit && hit.collider != null)
		{
			return new ViewCastInfo(true, hit.point, hit.distance, globalAngle);
		}
		return new ViewCastInfo(false, fovPos + dir * viewRadius, viewRadius, globalAngle);
	}

	public Vector3 DirFromAngle(float angle, bool angleIsGlobal)
	{
		if (angleIsGlobal)
		{
			angle -= transform.eulerAngles.z;
		}

		angle -= transform.eulerAngles.z;

		return new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad), Mathf.Cos(angle * Mathf.Deg2Rad), 0);
	}

	public struct Line
	{
		public Vector2 start;
		public Vector2 end;

		public Line(Vector2 start, Vector2 end)
		{
			this.start = start;
			this.end = end;
		}
	}

	public struct ViewCastInfo
	{
		public bool hit;
		public Vector3 point;
		public float dst;
		public float angle;

		public ViewCastInfo(bool _hit, Vector3 _point, float _dst, float _angle)
		{
			hit = _hit;
			point = _point;
			dst = _dst;
			angle = _angle;
		}
	}

	public struct EdgeInfo
	{
		public Vector3 pointA;
		public Vector3 pointB;

		public EdgeInfo(Vector3 _pointA, Vector3 _pointB)
		{
			pointA = _pointA;
			pointB = _pointB;
		}
	}
}