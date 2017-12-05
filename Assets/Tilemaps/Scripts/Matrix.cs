using System.Collections.Generic;
using System.Linq;
using Tilemaps.Scripts.Behaviours.Layers;
using Tilemaps.Scripts.Behaviours.Objects;
using Tilemaps.Scripts.Tiles;
using Tilemaps.Scripts.Utils;
using UnityEngine;

namespace Tilemaps.Scripts
{
    public enum TileType
    {
        All,
        Door,
        Wall,
        Window,
        Floor,
        Item,
        Player
    }

    public class Matrix : MonoBehaviour
    {
        public static Matrix GetMatrix(MonoBehaviour behaviour)
        {
            var matrix = behaviour.GetComponentInParent<Matrix>();
            return matrix;
        }

        private MetaTileMap _metaTileMap;
        private TileList _objects;

        private void Start()
        {
            _metaTileMap = GetComponent<MetaTileMap>();
            _objects = ((ObjectLayer)_metaTileMap.Layers[LayerType.Objects]).Objects;
        }

        public bool IsPassableAt(Vector3Int origin, Vector3Int position) => _metaTileMap.IsPassableAt(origin, position);

        public bool IsPassableAt(Vector3Int position) => _metaTileMap.IsPassableAt(position);

        public bool IsAtmosPassableAt(Vector3Int position) => _metaTileMap.IsAtmosPassableAt(position);

        public bool IsSpaceAt(Vector3Int position) => _metaTileMap.IsSpaceAt(position);

        public IEnumerable<T> Get<T>(Vector3Int position) where T : MonoBehaviour
        {
            return _objects.Get(position).Select(x => x.GetComponent<T>()).Where(x => x != null);
        }

        public T GetFirst<T>(Vector3Int position) where T : MonoBehaviour
        {
            return _objects.GetFirst(position)?.GetComponent<T>();
        }

        public IEnumerable<T> Get<T>(Vector3Int position, ObjectType type) where T : MonoBehaviour
        {
            return _objects.Get(position, type).Select(x => x.GetComponent<T>()).Where(x => x != null);
        }

        public bool ContainsAt(Vector3Int position, GameObject gameObject)
        {
            var registerTile = gameObject.GetComponent<RegisterTile>();

            return registerTile && _objects.Get(position).Contains(registerTile);
        }
    }
}