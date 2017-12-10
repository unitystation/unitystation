using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using NUnit.Framework.Constraints;
using Tilemaps.Scripts.Behaviours.Layers;
using Tilemaps.Scripts.Behaviours.Objects;
using Tilemaps.Scripts.Tiles;
using Tilemaps.Scripts.Utils;
using UnityEngine;

namespace Tilemaps.Scripts
{
    public class Matrix : MonoBehaviour
    {
        public static Matrix GetMatrix(MonoBehaviour behaviour)
        {
            var matrix = behaviour.GetComponentInParent<Matrix>();

            if (matrix == null)
            {
                behaviour.transform.parent = GameObject.FindGameObjectWithTag("SpawnParent").transform;
                matrix = behaviour.transform.parent.GetComponentInParent<Matrix>();
            }
            if(matrix == null){
                Debug.LogError("Matrix still null for: " + behaviour.gameObject.name +
                               " with parent: " + behaviour.transform.parent.name);
            }
            return matrix;
        }

        private MetaTileMap metaTileMap;
        private TileList objects;

        private void Start()
        {
            metaTileMap = GetComponent<MetaTileMap>();
            objects = ((ObjectLayer)metaTileMap.Layers[LayerType.Objects]).Objects;
        }

        public bool IsPassableAt(Vector3Int origin, Vector3Int position) => metaTileMap.IsPassableAt(origin, position);

        public bool IsPassableAt(Vector3Int position) => metaTileMap.IsPassableAt(position);

        public bool IsAtmosPassableAt(Vector3Int position) => metaTileMap.IsAtmosPassableAt(position);

        public bool IsSpaceAt(Vector3Int position) => metaTileMap.IsSpaceAt(position);

        public bool IsEmptyAt(Vector3Int position) => metaTileMap.IsEmptyAt(position);

        public bool IsFloatingAt(Vector3Int position)
        {
            var bounds = new BoundsInt(position - new Vector3Int(1, 1, 0), new Vector3Int(3, 3, 1));
            foreach (var pos in bounds.allPositionsWithin)
            {
                if (!metaTileMap.IsEmptyAt(pos))
                {
                    return false;
                }
            }
            return true;
        }

        public IEnumerable<T> Get<T>(Vector3Int position) where T : MonoBehaviour
        {
            return objects.Get(position).Select(x => x.GetComponent<T>()).Where(x => x != null);
        }

        public T GetFirst<T>(Vector3Int position) where T : MonoBehaviour
        {
            return objects.GetFirst(position)?.GetComponent<T>();
        }

        public IEnumerable<T> Get<T>(Vector3Int position, ObjectType type) where T : MonoBehaviour
        {
            return objects.Get(position, type).Select(x => x.GetComponent<T>()).Where(x => x != null);
        }

        public bool ContainsAt(Vector3Int position, GameObject gameObject)
        {
            var registerTile = gameObject.GetComponent<RegisterTile>();

            return registerTile && objects.Get(position).Contains(registerTile);
        }
    }
}