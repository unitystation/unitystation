using Mirror;
using UnityEngine;
using US13.Player;
using US13.Tilemaps.Behaviours.Layers;
using UniversalObjectPhysics = US13.Core.Physics.UniversalObjectPhysics;

namespace US13.Objects
{
	public class EnterTileBase : NetworkBehaviour, IPlayerEntersTile, IObjectEntersTile
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
			objectPhysics = GetComponent<UniversalObjectPhysics>();
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

		public virtual void OnDestroy()
		{
			if (previousMatrix != null)
			{
				previousMatrix.MetaTileMap.ObjectLayer.EnterTileBaseList.Remove(previousLocation, this);
			}
		}
	}
}