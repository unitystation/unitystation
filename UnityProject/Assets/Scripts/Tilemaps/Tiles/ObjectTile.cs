using System;
using UnityEngine;
using UnityEngine.Tilemaps;
using Systems.Electricity;
using Objects.Electrical;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Tiles
{
	[Serializable]
	public class ObjectTile : BasicTile
	{
		private TileChangeManager tileChangeManager;
		private GameObject objectCurrent;
		public bool IsItem;
		public bool KeepOrientation;
		public GameObject Object;
		public bool Offset;
		public bool Rotatable;

		[Header("Wire Stuff:")]
		public bool IsWire;
		public Connection WireEndA;
		public Connection WireEndB;
		public WiringColor CableType;
		public Sprite wireSprite;

#if UNITY_EDITOR

		// private void OnValidate()
		// {
		// 	if (Object != null)
		// 	{
		// 		if (objectCurrent == null)
		// 		{
		// 			// if sprite already exists (e.g. at startup), then load it, otherwise create a new one
		// 			EditorApplication.delayCall += () =>
		// 			{
		// 				if (IsWire && wireSprite != null)
		// 				{
		// 					PreviewSprite = wireSprite;
		// 				}
		// 				else
		// 				{
		// 					//PreviewSprite = PreviewSpriteBuilder.LoadSprite(Object);
		// 					//PreviewSpriteBuilder.Create(Object);
		// 				}
		// 			};
		// 		}
		// 		else if (Object != objectCurrent)
		// 		{
		// 			// from one object -> other (overwrite current sprite)
		// 			EditorApplication.delayCall += () => { PreviewSprite = PreviewSpriteBuilder.Create(Object); };
		// 		}
		// 	}
		// 	else if (objectCurrent != null)
		// 	{
		// 		// setting to None object (delete current sprite)
		// 		GameObject obj = objectCurrent;
		// 		EditorApplication.delayCall += () => { PreviewSpriteBuilder.DeleteSprite(obj); };
		// 	}
		//
		// 	objectCurrent = Object;
		//
		// 	if (objectCurrent != null && objectCurrent.GetComponentInChildren<RegisterItem>() != null)
		// 	{
		// 		IsItem = true;
		// 	}
		// }
#endif

		public void SpawnObject(Vector3Int position, Tilemap tilemap, Matrix4x4 transformMatrix)
		{
			if (!Object)
			{
				return;
			}


#if UNITY_EDITOR
			GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(Object);
#else
		GameObject go = Instantiate(Object);
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

			go.SetActive(true);

			if (IsWire)
			{
				SetWireSettings(go);
			}
		}

		private void SetWireSettings(GameObject spawnedObj)
		{
			var wireScript = spawnedObj.GetComponent<ElectricalOIinheritance>();
			wireScript.SetConnPoints(WireEndA, WireEndB);
			var SpriteScript = spawnedObj.GetComponent<CableInheritance>();
			SpriteScript.SetDirection(WireEndA, WireEndB, CableType);
		}

		public override Matrix4x4 Rotate(Matrix4x4 transformMatrix, bool anticlockwise = true, int count = 1)
		{
			if (Rotatable && !IsWire)
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
