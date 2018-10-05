﻿using System.Collections.Generic;
using System.Linq;
using UnityEngine;


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

		public override void RemoveTile(Vector3Int position)
		{
			foreach (RegisterTile obj in Objects.Get(position).ToArray())
			{
				DestroyImmediate(obj.gameObject);
			}

			base.RemoveTile(position);
		}

		public override bool IsPassableAt(Vector3Int origin, Vector3Int to)
		{
			List<RegisterTile> objectsTo = Objects.Get<RegisterTile>(to);

			if (!objectsTo.All(o => o.IsPassable(origin)))
			{
				return false;
			}

			List<RegisterTile> objectsOrigin = Objects.Get<RegisterTile>(origin);

			return objectsOrigin.All(o => o.IsPassable(origin)) && base.IsPassableAt(origin, to);
		}

		public override bool IsAtmosPassableAt(Vector3Int origin, Vector3Int to)
		{
			List<RegisterTile> objectsTo = Objects.Get<RegisterTile>(to);

            if (!objectsTo.All(o => o.IsAtmosPassable()))
			{
				return false;
			}
			
			List<RegisterTile> objectsOrigin = Objects.Get<RegisterTile>(origin);

			return objectsOrigin.All(o => o.IsAtmosPassable()) && base.IsAtmosPassableAt(origin, to);
		}

		public override bool IsSpaceAt(Vector3Int position)
		{
			return IsAtmosPassableAt(position, position) && base.IsSpaceAt(position);
		}

		public override void ClearAllTiles()
		{
			foreach (RegisterTile obj in Objects.AllObjects)
			{
				if (obj != null)
				{
					DestroyImmediate(obj.gameObject);
				}
			}

			base.ClearAllTiles();
		}
	}
