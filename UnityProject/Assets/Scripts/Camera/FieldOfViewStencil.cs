using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[Obsolete] //No longer used
public class FieldOfViewStencil : MonoBehaviour
{
	public float ViewRadius;
	[Range(0, 360)]
	public float ViewAngle;

	public LayerMask ObstacleMask;

	public float MeshResolution;
	public int EdgeResolveIterations;
	public float EdgeDistanceThreshhold;

	public float MaskCutawayDst;
	private Dictionary<Vector3Int, Tilemap> hitWalls = new Dictionary<Vector3Int, Tilemap>();
	private HashSet<Vector3Int> curWalls = new HashSet<Vector3Int>();

	private HashSet<GameObject> hitDoors = new HashSet<GameObject>();
	private HashSet<GameObject> curDoors = new HashSet<GameObject>();
	private MeshBuffer mMeshBuffer;

	List<Vector3> viewPoints = new List<Vector3>(400);

	RaycastHit2D hit;

	public MeshFilter ViewMeshFilter;

	public bool AffectWalls = false;

	float waitTime = 0f;

	void Start()
	{
		if (mMeshBuffer == null)
			mMeshBuffer = new MeshBuffer();

		ViewMeshFilter.mesh = mMeshBuffer.mesh;
	}

	void Update()
	{
        if (AffectWalls)
        {
			waitTime += Time.deltaTime;
			if (waitTime > 0.2f)
			{
				waitTime = 0f;
				CheckHitWallsCache();
			}
        }
    }

    private void LateUpdate()
    {
        DrawFieldOfView();
    }

    void CheckHitWallsCache()
    {
		var missingWalls = hitWalls.Keys.Except(curWalls).ToList();

	    if (missingWalls.Any())
	    {
		    for (int i = 0; i < missingWalls.Count(); i++)
		    {
			    //Tile newTile = (Tile)ScriptableObject.CreateInstance("Tile");
			    //newTile.sprite = SpriteManager.Instance.shroudSprite;
			    //hitWalls[missingWalls[i]].SetTile(missingWalls[i], newTile);
			    //hitWalls.Remove(missingWalls[i]);

			    hitWalls[missingWalls[i]].SetTile(missingWalls[i], null);
			    hitWalls.Remove(missingWalls[i]);
		    }
	    }

		curWalls.Clear();

		var missingDoors = hitDoors.Except(curDoors).ToList();
		for (int i = 0; i < missingDoors.Count(); i++)
		{
			missingDoors[i].SendMessage("TurnOnDoorFov", null, SendMessageOptions.DontRequireReceiver);
			hitDoors.Remove(missingDoors[i]);
		}

		curDoors.Clear();
	}

	void DrawFieldOfView()
	{
		int stepCount = Mathf.RoundToInt(ViewAngle * MeshResolution);
		float stepAngleSize = ViewAngle / stepCount;


		ViewCastInfo oldViewCast = new ViewCastInfo();

		for (int i = 0; i <= stepCount; i++)
		{
			float angle = transform.eulerAngles.z - ViewAngle / 2 + stepAngleSize * i;
			ViewCastInfo newViewCast = ViewCast(angle);

			if (i > 0)
			{
				bool edgeDstThreshholdExceeded = Mathf.Abs(oldViewCast.dst - newViewCast.dst) > EdgeDistanceThreshhold;

				if (oldViewCast.hit != newViewCast.hit || (oldViewCast.hit && edgeDstThreshholdExceeded))
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

		int pointCount = viewPoints.Count + 1;

		if (pointCount > mMeshBuffer.bufferSize)
			mMeshBuffer.bufferSize = pointCount;

		// Fill.
		mMeshBuffer.vertices[0] = Vector3.zero;

		for (int index = 0; index < mMeshBuffer.vertices.Length - 1; index++)
		{
			if (index < viewPoints.Count)
			{
				mMeshBuffer.vertices[index + 1] = transform.InverseTransformPoint(viewPoints[index]); // + Vector3.up * maskCutawayDst;
			}
			else
			{
				mMeshBuffer.vertices[index + 1] = Vector3.zero;
			}
		}

		mMeshBuffer.Update();

		viewPoints.Clear();
	}

	EdgeInfo FindEdge(ViewCastInfo minViewCast, ViewCastInfo maxViewCast)
	{
		float minAngle = minViewCast.angle;
		float maxAngle = maxViewCast.angle;
		Vector3 minPoint = Vector3.zero;
		Vector3 maxPoint = Vector3.zero;

		for (int i = 0; i < EdgeResolveIterations; i++)
		{
			float angle = (minAngle + maxAngle) / 2;
			ViewCastInfo newViewCast = ViewCast(angle);

			bool edgeDstThreshholdExceeded = Mathf.Abs(minViewCast.dst - newViewCast.dst) > EdgeDistanceThreshhold;

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

	ViewCastInfo ViewCast(float globalAngle)
	{
		Vector3 dir = DirFromAngle(globalAngle, true);
		hit = Physics2D.Raycast(transform.position, dir, ViewRadius, ObstacleMask);
		//	Debug.DrawRay(transform.position, dir * 40f, Color.red, 0.1f);
		if (hit && hit.collider != null)
		{
			Vector3 hitPosition = Vector3.zero;

			//Hit a closed door (Layer 17)
			if(hit.collider.gameObject.layer == 17 && AffectWalls)
			{
				if(!hitDoors.Contains(hit.collider.gameObject))
				{
					hit.collider.gameObject.SendMessage("TurnOffDoorFov", null, SendMessageOptions.DontRequireReceiver);
					hitDoors.Add(hit.collider.gameObject);
				}

				if (!curDoors.Contains(hit.collider.gameObject))
				{
					curDoors.Add(hit.collider.gameObject);
				}
			}

			//Hit a wall (layer 9):
			if (hit.collider.gameObject.layer == 9 && AffectWalls)
			{
				hitPosition = Vector3Int.RoundToInt(hit.point + ((Vector2)dir * 0.5f));
				Tilemap wallTilemap = MatrixManager.Instance.wallsTileMaps[hit.collider];
				Vector3Int wallCellPos = wallTilemap.WorldToCell(hitPosition);

				if (!hitWalls.ContainsKey(wallCellPos))
				{
					//Tilemap fxTileMap = MatrixManager.Instance.wallsToTopLayerFX[hit.collider];
					///// Check that there actually is a tile on the wall Tilemap as the hitPosition isn't accurate
					//TileBase getTile = wallTilemap.GetTile(wallCellPos);
					//if (getTile != null)
					//{
					//	Tile newTile = (Tile)ScriptableObject.CreateInstance("Tile");
					//	newTile.sprite = SpriteManager.Instance.shroudSprite;
					//	fxTileMap.SetTile(wallCellPos, newTile);
					//	hitWalls.Add(wallCellPos, fxTileMap);

					//	//fxTileMap.SetTile(fxTileMap.WorldToCell(hitPosition), null);
					//	//hitWalls.Add(wallCellPos, fxTileMap);
					//}
				}
				if (!curWalls.Contains(wallCellPos))
				{
					curWalls.Add(wallCellPos);
				}
			}

			return new ViewCastInfo(true, hit.point, hit.distance, globalAngle);
		}
		else
		{
			return new ViewCastInfo(false, transform.position + dir * ViewRadius, ViewRadius, globalAngle);
		}
	}

	public Vector3 DirFromAngle(float angle, bool angleIsGlobal)
	{
		if (angleIsGlobal) {
			angle -= transform.eulerAngles.z;
		}

		angle -= transform.eulerAngles.z;

		return new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad), Mathf.Cos(angle * Mathf.Deg2Rad), 0);
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

	private class MeshBuffer
	{
		public Vector3[] vertices;
		public int[] triangles;

		private const int DefaultSize = 1500;

		private int mBufferSize;

		public MeshBuffer()
		{
			mesh = new Mesh { name = "Buffered View Mesh" };

			bufferSize = DefaultSize;
		}

		public int bufferSize
		{
			get
			{
				return mBufferSize;
			}

			set
			{
				if (mBufferSize == value)
					return;

				mBufferSize = value;

				RebuildVertices();

				RebuildTriangles();
			}
		}

		public void Update()
		{
			mesh.vertices = vertices;
		}

		private void RebuildVertices()
		{
			// Set array size.
			int _vertsSize = bufferSize + 1;

			if (vertices == null)
			{
				vertices = new Vector3[_vertsSize];
			}
			if (vertices.Length < _vertsSize)
			{
				Array.Resize(ref vertices, _vertsSize);
			}

			mesh.vertices = vertices;
		}

		private void RebuildTriangles()
		{
			int _triangleSize = (bufferSize - 2) * 3;

			// Set array size.
			if (triangles == null)
			{
				triangles = new int[_triangleSize];
			}
			if (triangles.Length < _triangleSize)
			{
				Array.Resize(ref triangles, _triangleSize);
			}

			// Fill data.
			for (int index = 0; index < bufferSize - 2; index++)
			{
				//if (index < bufferSize - 2)
				{
					triangles[index * 3] = 0;
					triangles[index * 3 + 1] = index + 1;
					triangles[index * 3 + 2] = index + 2;
				}
			}

			// Apply data.
			mesh.triangles = triangles;
		}

		public Mesh mesh { get; private set; }
	}
}