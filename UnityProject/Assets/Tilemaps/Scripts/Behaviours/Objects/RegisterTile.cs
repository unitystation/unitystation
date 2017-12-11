using System.Collections;
using Tilemaps.Scripts.Behaviours.Layers;
using UnityEngine;

namespace Tilemaps.Scripts.Behaviours.Objects
{
    public enum ObjectType
    {
        Item,
        Object,
        Player
    }

    [ExecuteInEditMode]
    public abstract class RegisterTile : MonoBehaviour
    {
        public bool IsRegistered { get; private set; }

        public ObjectType ObjectType;

        private ObjectLayer layer;

        private Vector3Int _position;

        public Vector3Int Position
        {
            get { return _position; }
            private set
            {
                layer?.Objects.Remove(_position, this);
                layer?.Objects.Add(value, this);
                _position = value;
            }
        }

        public void Start()
        {
            layer = transform.GetComponentInParent<ObjectLayer>();
            
            if (layer == null)
            {
                GameObject tempParent = GameObject.FindGameObjectWithTag("SpawnParent");
				//FIXME: Still issues with init for registering objects. Sometimes SpawnParent tag cannot be found
				// Suggestion: Move to a Matrix Manager system
				if(tempParent != null){
					transform.parent = tempParent.transform;
				} else {
					Debug.LogError("GameObject: " + gameObject.name + " could not find the matrix");
				}
                layer = transform.GetComponentInParent<ObjectLayer>();
            }
            
            Register();
        }

        private void OnEnable()
        {
            // In case of recompilation and Start doesn't get called
            layer?.Objects.Add(Position, this);
            IsRegistered = true;
        }

        private void OnDisable()
        {
            Unregister();
        }

        public void OnDestroy()
        {
            layer?.Objects.Remove(Position, this);
        }

        public void UpdatePosition()
        {
            Position = Vector3Int.FloorToInt(transform.localPosition);
        }

        public void Register()
        {
            UpdatePosition();
            IsRegistered = true;
        }

        public void Unregister()
        {
            layer?.Objects.Remove(Position, this);
            IsRegistered = false;
        }

        public virtual bool IsPassable()
        {
            return true;
        }

        public virtual bool IsPassable(Vector3Int to)
        {
            return true;
        }

        public virtual bool IsAtmosPassable()
        {
            return true;
        }
    }
}
