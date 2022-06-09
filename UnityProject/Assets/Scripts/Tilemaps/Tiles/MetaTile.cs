using System.Collections.Generic;
using Tiles;
#if UNITY_EDITOR
using UnityEditor;
#endif


public class MetaTile : GenericTile
	{
		private LayerTile _baseCurrent;
		private LayerTile _floorCurrent;
		private LayerTile _objectCurrent;
		private LayerTile _grillCurrent;
		private LayerTile _structureCurrent;
		public LayerTile Base;
		public LayerTile Floor;
		public LayerTile Object;
		public LayerTile Grill; //Has to be separate here to get it off objects layer
		public LayerTile Structure;

#if UNITY_EDITOR
		private void OnValidate()
		{
			CheckTileType(ref Structure, LayerType.Walls, LayerType.Windows);
			CheckTileType(ref Object, LayerType.Objects);
			CheckTileType(ref Floor, LayerType.Floors);
			CheckTileType(ref Grill, LayerType.Grills);
			CheckTileType(ref Base, LayerType.Base);

			if (Structure != _structureCurrent || Object != _objectCurrent || Floor != _floorCurrent ||
			    Base != _baseCurrent || Grill != _grillCurrent)
			{
				if (_structureCurrent == null && _objectCurrent == null && _floorCurrent == null &&
				    _baseCurrent == null && _grillCurrent == null)
				{
					// if everything is null, it could be that it's loading on startup, so there already should be an preview sprite to load
					EditorApplication.delayCall += () =>
					{
						PreviewSprite = PreviewSpriteBuilder.LoadSprite(this) ?? PreviewSpriteBuilder.Create(this);
						;
					};
				}
				else
				{
					// something changed, so create a new preview sprite
					EditorApplication.delayCall += () => { PreviewSprite = PreviewSpriteBuilder.Create(this); };
				}
			}

			_structureCurrent = Structure;
			_objectCurrent = Object;
			_floorCurrent = Floor;
			_baseCurrent = Base;
			_grillCurrent = Grill;
		}
#endif

		private static void CheckTileType(ref LayerTile tile, params LayerType[] requiredTypes)
		{
			if (tile != null)
			{
				foreach (LayerType requiredType in requiredTypes)
				{
					if (tile.LayerType == requiredType)
					{
						return;
					}
				}
				tile = null;
			}
		}

		public IEnumerable<LayerTile> GetTiles()
		{
			List<LayerTile> list = new List<LayerTile>();

			if (Base)
			{
				list.Add(Base);
			}
			if (Floor)
			{
				list.Add(Floor);
			}
			if (Object)
			{
				list.Add(Object);
			}
			if (Grill)
			{
				list.Add(Grill);
			}
			if (Structure)
			{
				list.Add(Structure);
			}

			return list.ToArray();
		}
	}
