using System.Collections.Generic;
using System.Linq;
using Tilemaps.Scripts.Behaviours.Objects;
using UnityEngine;

namespace Tilemaps.Scripts.Utils
{
    public class TileList
    {
        private Dictionary<Vector3Int, List<RegisterTile>> _objects = new Dictionary<Vector3Int, List<RegisterTile>>();
        
        public List<RegisterTile> AllObjects => _objects.Values.SelectMany(x => x).ToList();

        public void Add(Vector3Int position, RegisterTile obj)
        {
            if (!_objects.ContainsKey(position))
            {
                _objects[position] = new List<RegisterTile>();
            }

            if (!_objects[position].Contains(obj))
            {
                _objects[position].Add(obj);
            }
        }

        public List<RegisterTile> Get(Vector3Int position)
        {
            return _objects.ContainsKey(position) ? _objects[position] : new List<RegisterTile>();
        }

        public List<T> Get<T>(Vector3Int position) where T : RegisterTile
        {
            return Get(position).OfType<T>().ToList();
        }

        public T GetFirst<T>(Vector3Int position) where T : RegisterTile
        {
            var objects = Get(position).OfType<T>().ToArray();
            return objects.FirstOrDefault();
        }

        public void Remove(Vector3Int position, RegisterTile obj = null)
        {
            if (_objects.ContainsKey(position))
            {
                if (obj == null)
                {
                    _objects[position].Clear();
                }
                else
                {
                    _objects[position].Remove(obj);
                }
            }
        }
    }
}