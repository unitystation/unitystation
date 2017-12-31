using System;
using Tilemaps.Behaviours.Objects;
using Tilemaps.Scripts.Utils;
using UnityEngine;
using UnityEngine.Tilemaps;
#if UNITY_EDITOR
using UnityEditor;

#endif

namespace Tilemaps.Scripts.Tiles
{
	[Serializable]
	public class ObjectTile : LayerTile
	{
		private GameObject _objectCurrent;
		public bool IsItem;
		public bool KeepOrientation;
		public GameObject Object;
		public bool Offset;
		public bool Rotatable;

#if UNITY_EDITOR
		private void OnValidate()
		{
			if (Object != null)
			{
				if (_objectCurrent == null)
				{
					// if sprite already exists (e.g. at startup), then load it, otherwise create a new one
					EditorApplication.delayCall += () =>
					{
						PreviewSprite = PreviewSpriteBuilder.LoadSprite(Object) ??
						                PreviewSpriteBuilder.Create(Object);
					};
				}
				else if (Object != _objectCurrent)
				{
					// from one object -> other (overwrite current sprite)
					EditorApplication.delayCall += () => { PreviewSprite = PreviewSpriteBuilder.Create(Object); };
				}
			}
			else if (_objectCurrent != null)
			{
				// setting to None object (delete current sprite)
				GameObject obj = _objectCurrent;
				EditorApplication.delayCall += () => { PreviewSpriteBuilder.DeleteSprite(obj); };
			}

			_objectCurrent = Object;

			if (_objectCurrent != null && _objectCurrent.GetComponentInChildren<RegisterItem>() != null)
			{
				IsItem = true;
			}
		}
#endif

		public void SpawnObject(Vector3Int position, Tilemap tilemap, Matrix4x4 transformMatrix)
		{
			if (!Object)
			{
				return;
			}

#if UNITY_EDITOR
			GameObject go = (GameObject) PrefabUtility.InstantiatePrefab(Object);
#else
            var go = Instantiate(Object);
#endif

			go.SetActive(false);
			go.transform.parent = tilemap.transform;

			Vector3 objectOffset = !Offset ? Vector3.zero : transformMatrix.rotation * Vector3.up;

			go.transform.localPosition = position + objectOffset;
			go.transform.rotation = tilemap.transform.rotation;

			if (!KeepOrientation)
			{
				go.transform.rotation *= transformMatrix.rotation;
			}

			go.name = Object.name;

			if (IsItem)
			{
			}
			else
			{
				RegisterObject registerObject = go.GetComponent<RegisterObject>() ?? go.AddComponent<RegisterObject>();
				registerObject.Offset = Vector3Int.RoundToInt(-objectOffset);
			}


			go.SetActive(true);
		}

		public override Matrix4x4 Rotate(Matrix4x4 transformMatrix, bool anticlockwise = true, int count = 1)
		{
			if (Rotatable)
			{
				for (int i = 0; i < count; i++)
				{
					transformMatrix = RotateOnce(transformMatrix, anticlockwise);
				}
				return transformMatrix;
			}
			return base.Rotate(transformMatrix, anticlockwise, count);
		}

		private Matrix4x4 RotateOnce(Matrix4x4 transformMatrix, bool anticlockwise)
		{
			Quaternion rotation = Quaternion.Euler(0f, 0f, anticlockwise ? 90f : -90f);

			Quaternion newRotation = KeepOrientation ? Quaternion.identity : transformMatrix.rotation * rotation;
			Vector3 newTranslation = !Offset ? Vector3.zero : rotation * transformMatrix.GetColumn(3);

			if (Offset && transformMatrix.Equals(Matrix4x4.identity))
			{
				newTranslation = Vector3.left;
			}

			return Matrix4x4.TRS(newTranslation, newRotation, Vector3.one);
		}
	}
}