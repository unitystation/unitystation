using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


	[ExecuteInEditMode]
	public class MetaTileMap : MonoBehaviour
	{
		public Dictionary<LayerType, Layer> Layers { get; private set; }
		//Lists for speed
		private List<LayerType> LayersKeys { get; set; }
		private List<Layer> LayersValues { get; set; }

		private Matrix matrix;
		private Matrix Matrix => matrix ? matrix : ( matrix = GetComponent<Matrix>() );

		private void OnEnable()
		{
			Layers = new Dictionary<LayerType, Layer>();
			LayersKeys = new List<LayerType>();
			LayersValues = new List<Layer>();

			foreach (Layer layer in GetComponentsInChildren<Layer>(true))
			{
				Layers[layer.LayerType] = layer;
				LayersKeys.Add( layer.LayerType );
				LayersValues.Add( layer );
			}
		}

        public bool IsPassableAt(Vector3Int position)
        {
            return IsPassableAt(position, position);
        }

		public bool IsPassableAt( Vector3Int origin, Vector3Int to, bool inclPlayers = true, GameObject context = null )
		{
			Vector3Int toX = new Vector3Int(to.x, origin.y, origin.z);
			Vector3Int toY = new Vector3Int(origin.x, to.y, origin.z);

			return _IsPassableAt(origin, toX, inclPlayers, context) && _IsPassableAt(toX, to, inclPlayers, context) ||
			        _IsPassableAt(origin, toY, inclPlayers, context) && _IsPassableAt(toY, to, inclPlayers, context);
		}

		private bool _IsPassableAt( Vector3Int origin, Vector3Int to, bool inclPlayers = true, GameObject context = null ) {
			for ( var i = 0; i < LayersValues.Count; i++ ) {
				if ( !LayersValues[i].IsPassableAt( origin, to, inclPlayers, context ) ) {
					return false;
				}
			}

			return true;
		}

		public bool IsAtmosPassableAt(Vector3Int position)
		{
			return IsAtmosPassableAt(position, position);
		}

		public bool IsAtmosPassableAt(Vector3Int origin, Vector3Int to)
		{
			Vector3Int toX = new Vector3Int(to.x, origin.y, origin.z);
			Vector3Int toY = new Vector3Int(origin.x, to.y, origin.z);

			return _IsAtmosPassableAt(origin, toX) && _IsAtmosPassableAt(toX, to) ||
					_IsAtmosPassableAt(origin, toY) && _IsAtmosPassableAt(toY, to);
		}

		private bool _IsAtmosPassableAt(Vector3Int origin, Vector3Int to) {
			for ( var i = 0; i < LayersValues.Count; i++ ) {
				if ( !LayersValues[i].IsAtmosPassableAt( origin, to ) ) {
					return false;
				}
			}

			return true;
		}

		public bool IsSpaceAt(Vector3Int position) {
			for ( var i = 0; i < LayersValues.Count; i++ ) {
				if ( !LayersValues[i].IsSpaceAt( position ) ) {
					return false;
				}
			}

			return true;
		}

		public void SetTile(Vector3Int position, LayerTile tile, Matrix4x4 transformMatrix)
		{
			Layers[tile.LayerType].SetTile(position, tile, transformMatrix);
		}

		public LayerTile GetTile(Vector3Int position, LayerType layerType)
		{
			Layer layer = null;
			Layers.TryGetValue(layerType, out layer);
			return layer ? Layers[layerType].GetTile(position) : null;
		}

		public LayerTile GetTile(Vector3Int position) {
			for ( var i = 0; i < LayersValues.Count; i++ ) {
				LayerTile tile = LayersValues[i].GetTile( position );
				if ( tile != null ) {
					return tile;
				}
			}

			return null;
		}

		public bool IsEmptyAt(Vector3Int position) {
			for ( var index = 0; index < LayersKeys.Count; index++ ) {
				LayerType layer = LayersKeys[index];
				if ( layer != LayerType.Objects && HasTile( position, layer ) ) {
					return false;
				}

				if ( layer == LayerType.Objects ) {
					var objects = Matrix.Get<RegisterTile>( position );
					for ( var i = 0; i < objects.Count; i++ ) {
						var o = objects[i];
						if ( !o.IsPassable() ) {
							return false;
						}
					}
				}
			}

			return true;
		}
		public bool IsNoGravityAt(Vector3Int position) {
			for ( var i = 0; i < LayersKeys.Count; i++ ) {
				LayerType layer = LayersKeys[i];
				if ( layer != LayerType.Objects && HasTile( position, layer ) ) {
					return false;
				}
			}

			return true;
		}

		public bool IsEmptyAt(GameObject[] context, Vector3Int position) {
			for ( var i1 = 0; i1 < LayersKeys.Count; i1++ ) {
				LayerType layer = LayersKeys[i1];
				if ( layer != LayerType.Objects && HasTile( position, layer ) ) {
					return false;
				}

				if ( layer == LayerType.Objects ) {
					var objects = Matrix.Get<RegisterTile>( position );
					for ( var i = 0; i < objects.Count; i++ ) {
						var o = objects[i];
						if ( !o.IsPassable() ) {
							bool isExcluded = false;
							for ( var index = 0; index < context.Length; index++ ) {
								if ( o.gameObject == context[index] ) {
									isExcluded = true;
									break;
								}
							}

							if ( !isExcluded ) {
								return false;
							}
						}
					}
				}
			}

			return true;
		}

		public bool HasTile(Vector3Int position, LayerType layerType)
		{
			return Layers[layerType].HasTile(position);
		}

		public void RemoveTile(Vector3Int position, LayerType refLayer) {
			for ( var i = 0; i < LayersValues.Count; i++ ) {
				Layer layer = LayersValues[i];
				if ( layer.LayerType < refLayer &&
				     !( refLayer == LayerType.Objects &&
				        layer.LayerType == LayerType.Floors ) &&
				     refLayer != LayerType.Grills ) {
					layer.RemoveTile( position );
				}
			}
		}

		public void ClearAllTiles() {
			for ( var i = 0; i < LayersValues.Count; i++ ) {
				LayersValues[i].ClearAllTiles();
			}
		}

		public BoundsInt GetBounds()
		{
			Vector3Int minPosition = Vector3Int.one * int.MaxValue;
			Vector3Int maxPosition = Vector3Int.one * int.MinValue;

			for ( var i = 0; i < LayersValues.Count; i++ ) {
				BoundsInt layerBounds = LayersValues[i].Bounds;

				minPosition = Vector3Int.Min( layerBounds.min, minPosition );
				maxPosition = Vector3Int.Max( layerBounds.max, maxPosition );
			}

			return new BoundsInt(minPosition, maxPosition-minPosition);
		}


#if UNITY_EDITOR
		public void SetPreviewTile(Vector3Int position, LayerTile tile, Matrix4x4 transformMatrix)
		{
			for ( var i = 0; i < LayersValues.Count; i++ ) {
				Layer layer = LayersValues[i];
				if ( layer.LayerType < tile.LayerType ) {
					Layers[layer.LayerType].SetPreviewTile( position, LayerTile.EmptyTile, Matrix4x4.identity );
				}
			}

			if(!Layers.ContainsKey(tile.LayerType)){
				Debug.LogError($"LAYER TYPE: {tile.LayerType} not found!");
				return;
			}
			Layers[tile.LayerType].SetPreviewTile(position, tile, transformMatrix);
		}

		public void ClearPreview() {
			for ( var i = 0; i < LayersValues.Count; i++ ) {
				LayersValues[i].ClearPreview();
			}
		}
#endif
	}
