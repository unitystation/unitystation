using System.Collections.Generic;
using System.Linq;
using UnityEngine;


	/// <summary>
	/// ObjectLayer holds all the objects on all the tiles in the game world - specifically the RegisterTile components of those objects.
	/// It provides functionality for checking what should occur on given tiles, such as if a tile at a specific location should be passable.
	/// </summary>
	[ExecuteInEditMode]
	public class ObjectLayer : Layer
	{
		private TileList _objects;

		public TileList Objects => _objects ?? (_objects = new TileList());

		public override void SetTile(Vector3Int position, GenericTile tile, Matrix4x4 transformMatrix)
		{
			ObjectTile objectTile = tile as ObjectTile;

			if (objectTile)
			{
				if (!objectTile.IsItem)
				{
					tilemap.SetTile(position, null);
				}
				objectTile.SpawnObject(position, tilemap, transformMatrix);
			}
			else
			{
				base.SetTile(position, tile, transformMatrix);
			}
		}

		public override bool HasTile(Vector3Int position)
		{
			return Objects.Get(position).Count > 0 || base.HasTile(position);
		}

		public override void RemoveTile(Vector3Int position, bool removeAll=false)
		{
			List<RegisterTile> objs = Objects.Get(position);
			for ( var i = 0; i < objs.Count; i++ ) {
				RegisterTile obj = objs[i];
				DestroyImmediate( obj.gameObject );
			}

			base.RemoveTile(position, removeAll);
		}

		public override bool IsPassableAt( Vector3Int origin, Vector3Int to, bool inclPlayers = true, GameObject context = null )
		{
			//Targeting windoors here
			List<RegisterTile> objectsOrigin = Objects.Get(origin);
			for ( var i = 0; i < objectsOrigin.Count; i++ ) {
				if ( !objectsOrigin[i].IsPassableTo( to ) && ( !context || objectsOrigin[i].gameObject != context ) ) {
					//Can't get outside the tile because windoor doesn't allow us
					return false;
				}
			}

			List<RegisterTile> objectsTo = Objects.Get(to);
			bool toPass;
			if ( inclPlayers ) {
				toPass = true;
				for ( var i = 0; i < objectsTo.Count; i++ ) {
					RegisterTile o = objectsTo[i];
					if ( !o.IsPassable( origin ) && ( !context || o.gameObject != context ) ) {
						toPass = false;
						break;
					}
				}
			} else {
				toPass = true;
				for ( var i = 0; i < objectsTo.Count; i++ ) {
					RegisterTile o = objectsTo[i];
					if ( o.ObjectType != ObjectType.Player && !o.IsPassable( origin ) && ( !context || o.gameObject != context ) ) {
						toPass = false;
						break;
					}
				}
			}

			bool rods = base.IsPassableAt(origin, to, inclPlayers);

//			Logger.Log( $"IPA = {toPass} && {rods} @ {MatrixManager.Instance.LocalToWorldInt( origin, MatrixManager.Get(0).Matrix )} -> {MatrixManager.Instance.LocalToWorldInt( to, MatrixManager.Get(0).Matrix )} " +
//			            $" (in local: {origin} -> {to})", Category.Matrix );
			return toPass && rods;
		}

		public override bool IsAtmosPassableAt(Vector3Int origin, Vector3Int to)
		{
			List<RegisterTile> objectsTo = Objects.Get(to);

			for (int i = 0; i < objectsTo.Count; i++)
			{
				if (!objectsTo[i].IsAtmosPassable())
				{
					return false;
				}
			}

			List<RegisterTile> objectsOrigin = Objects.Get(origin);

			for (int i = 0; i < objectsOrigin.Count; i++)
			{
				if (!objectsOrigin[i].IsAtmosPassable())
				{
					return false;
				}
			}
			
			return base.IsAtmosPassableAt(origin, to);
		}

		public override bool IsSpaceAt(Vector3Int position)
		{
			return IsAtmosPassableAt(position, position) && base.IsSpaceAt(position);
		}

		public override void ClearAllTiles() {
			for ( var i = 0; i < Objects.AllObjects.Count; i++ ) {
				RegisterTile obj = Objects.AllObjects[i];
				if ( obj != null ) {
					DestroyImmediate( obj.gameObject );
				}
			}

			base.ClearAllTiles();
		}
	}
