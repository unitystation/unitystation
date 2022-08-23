using System;
using UnityEngine;

namespace Objects
{
	public class EnterTileBase : MonoBehaviour, IPlayerEntersTile, IObjectEntersTile
	{
		public virtual bool WillAffectPlayer(PlayerScript playerScript)
		{
			return true;
		}

		public virtual void OnPlayerStep(PlayerScript playerScript) { }

		public virtual bool WillAffectObject(GameObject eventData)
		{
			return true;
		}

		public virtual void OnObjectEnter(GameObject eventData) { }

		protected UniversalObjectPhysics objectPhysics;

		private Matrix previousMatrix;
		private Vector3Int previousLocation;

		protected virtual void Awake()
		{
			objectPhysics = GetComponent<UniversalObjectPhysics>();
		}

		protected virtual void OnEnable()
		{
			objectPhysics.OnLocalTileReached.AddListener(OnLocalPositionChangedServer);
		}

		protected virtual void OnDisable()
		{
			objectPhysics.OnLocalTileReached.RemoveListener(OnLocalPositionChangedServer);
		}

		public void OnLocalPositionChangedServer(Vector3Int oldLocalPos, Vector3Int newLocalPos)
		{
			if (objectPhysics.registerTile.Matrix.MetaTileMap.ObjectLayer.EnterTileBaseList == null) return;
			if (previousMatrix != null)
			{
				previousMatrix.MetaTileMap.ObjectLayer.EnterTileBaseList.Remove(previousLocation, this);
			}

			objectPhysics.registerTile.Matrix.MetaTileMap.ObjectLayer.EnterTileBaseList.Add(newLocalPos,this);
			previousLocation = newLocalPos;
			previousMatrix = objectPhysics.registerTile.Matrix;
		}

		public void OnDestroy()
		{
			if (previousMatrix != null)
			{
				previousMatrix.MetaTileMap.ObjectLayer.EnterTileBaseList.Remove(previousLocation, this);
			}
		}
	}
}