using System.Collections.Generic;
using System.Linq;
using Tilemaps.Behaviours.Objects;
using Tilemaps.Tiles;
using Tilemaps.Utils;
using UnityEngine;

namespace Tilemaps.Behaviours.Layers
{
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
			RegisterTile objTo = Objects.GetFirst<RegisterTile>(to);

			if (objTo && !objTo.IsPassable(origin))
			{
				return false;
			}

			RegisterTile objOrigin = Objects.GetFirst<RegisterTile>(origin);
			if (objOrigin && !objOrigin.IsPassable(to))
			{
				return false;
			}

			return base.IsPassableAt(origin, to);
		}

		public override bool IsPassableAt(Vector3Int position)
		{
			List<RegisterTile> objects = Objects.Get<RegisterTile>(position);

			return objects.All(x => x.IsPassable()) && base.IsPassableAt(position);
		}

		public override bool IsAtmosPassableAt(Vector3Int position)
		{
			RegisterTile obj = Objects.GetFirst<RegisterTile>(position);

			return obj ? obj.IsAtmosPassable() : base.IsAtmosPassableAt(position);
		}

		public override bool IsSpaceAt(Vector3Int position)
		{
			return IsAtmosPassableAt(position) && base.IsSpaceAt(position);
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
}