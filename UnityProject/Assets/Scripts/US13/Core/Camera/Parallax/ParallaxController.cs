using System;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using US13.Managers;
using US13.Managers.NetworkManagement;
using US13.Managers.UpdateManager;
using Random = System.Random;

namespace US13.Core.Camera.Parallax
{
	public class ParallaxController : MonoBehaviour
	{
		public ParallaxStars PrefabParallax;

		public int Columns;
		public int Rows;

		public List<ParallaxColumn> backgroundTiles = new();

		public List<ParallaxStars> PrePlaced;
		public UnityEngine.Camera targetCamera;
		public bool autoGenerateOnAwake = true;
		public List<ParallaxLayerDefinition> dynamicLayers = new();
		public List<Color> cameraPositionColors = new();
		public float cameraColorWorldScale = 0.002f;
		public float cameraColorPositionPrecision = 0.01f;

		public Vector2 tileBounds;
		private int centerColumn;
		private int centerRow;
		private Color lastCameraColor;
		private bool hasCameraColor;
		private SpriteRenderer[] cameraColorRenderers;
		private bool cameraColorRenderersCached;

		public Vector3 ObjectScale = Vector3.one;

		public float moveMultiplier = 1;

		public float zOffset = -1;

		private void Awake()
		{
			if (targetCamera == null) targetCamera = UnityEngine.Camera.main;

			if (!autoGenerateOnAwake) return;

			BuildTiles();
		}

		[Button]
		public void BuildTiles()
		{
			if (PrefabParallax == null) return;

			if (backgroundTiles == null) backgroundTiles = new List<ParallaxColumn>();

			if (backgroundTiles.Count > 0)
			{
				CacheCameraColorRenderers();
				return;
			}

			for (var i = 0; i < Columns; i++)
			{
				var Column = new ParallaxColumn();
				backgroundTiles.Add(Column);
				for (var j = 0; j < Rows; j++)
				{
					ParallaxStars Object = Instantiate(PrefabParallax, transform);
					Object.transform.localScale = ObjectScale;
					Column.rows.Add(Object);
				}
			}

			var random = new Random();
			if (PrePlaced != null)
			{
				foreach (ParallaxStars PrePlace in PrePlaced)
				{
					int randomColumn = random.Next(Columns);
					int randomRow = random.Next(Rows);
					Destroy(backgroundTiles[randomColumn].rows[randomRow].gameObject);
					backgroundTiles[randomColumn].rows[randomRow] = PrePlace;
				}
			}

			var rend = backgroundTiles[0].rows[0].GetComponent<SpriteRenderer>();
			if (rend != null)
			{
				tileBounds = rend.sprite.bounds.size * rend.sprite.pixelsPerUnit / 100f *
				             rend.transform.localScale.To2();
			}
			else
			{
				tileBounds = backgroundTiles[0].rows[0].transform.localScale.To2();
			}

			centerColumn = Columns / 2;
			centerRow = Rows / 2;
			CacheCameraColorRenderers();
			RealignTiles();
			UpdateCameraColor();
		}

		[Button]
		public void CreateDynamicLayers()
		{
			if (dynamicLayers == null || dynamicLayers.Count == 0) return;

			foreach (ParallaxLayerDefinition layerDef in dynamicLayers)
			{
				var layerRoot = new GameObject(layerDef.name);
				layerRoot.transform.SetParent(transform, false);
				layerRoot.transform.localPosition = Vector3.zero;

				var layerController = layerRoot.AddComponent<ParallaxController>();
				layerController.PrefabParallax = layerDef.prefab;
				layerController.Columns = layerDef.columns;
				layerController.Rows = layerDef.rows;
				layerController.ObjectScale = layerDef.objectScale;
				layerController.moveMultiplier = layerDef.moveMultiplier;
				layerController.zOffset = layerDef.zOffset;
				layerController.targetCamera = targetCamera != null ? targetCamera : UnityEngine.Camera.main;
				layerController.autoGenerateOnAwake = true;
				layerController.PrePlaced = layerDef.prePlaced;
				layerController.dynamicLayers = new List<ParallaxLayerDefinition>();
				layerController.cameraPositionColors = layerDef.cameraPositionColors;
				layerController.cameraColorWorldScale = layerDef.cameraColorWorldScale;
				layerController.cameraColorPositionPrecision = layerDef.cameraColorPositionPrecision;
			}
		}


		private void OnEnable()
		{
			if (CustomNetworkManager.IsHeadless) return;
			UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
		}

		private void OnDisable()
		{
			if (CustomNetworkManager.IsHeadless) return;
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
		}

		[Button]
		public void Rescale()
		{
			for (var i = 0; i < backgroundTiles.Count; i++)
			{
				for (var j = 0; j < backgroundTiles[0].rows.Count; j++)
				{
					backgroundTiles[i].rows[j].transform.localScale = ObjectScale;
				}
			}

			var rend = backgroundTiles[0].rows[0].GetComponent<SpriteRenderer>();
			if (rend != null)
			{
				tileBounds = rend.sprite.bounds.size * rend.sprite.pixelsPerUnit / 100f *
				             rend.transform.localScale.To2();
			}
			else
			{
				tileBounds = backgroundTiles[0].rows[0].transform.localScale.To2();
			}

			centerColumn = Columns / 2;
			centerRow = Rows / 2;

			RealignTilesEditor();
		}


		[Button]
		public void RealignTilesEditor()
		{
			RealignTiles(true);
			RealignTiles(false);
		}

		private void RealignTiles(bool? UpdateX = null)
		{
			if (targetCamera == null) targetCamera = UnityEngine.Camera.main;

			if (targetCamera == null || backgroundTiles == null || backgroundTiles.Count == 0) return;

			for (var i = 0; i < backgroundTiles.Count; i++)
			for (var j = 0; j < backgroundTiles[0].rows.Count; j++)
			{
				Vector3 newPos = targetCamera.transform.position;
				newPos.z = zOffset;
				if (UpdateX is true or null)
				{
					newPos.x = targetCamera.transform.position.x;
					newPos.x += (i - centerColumn) * tileBounds.x;
				}
				else
				{
					newPos.x = backgroundTiles[i].rows[j].transform.position.x;
				}


				if (UpdateX is false or null)
				{
					newPos.y = targetCamera.transform.position.y;
					newPos.y += (j - centerRow) * tileBounds.y;
				}
				else
				{
					newPos.y = backgroundTiles[i].rows[j].transform.position.y;
				}

				backgroundTiles[i].rows[j].transform.position = newPos;
			}

			CheckSpaceBackgroundObjects();
		}

		private void UpdateMe()
		{
			if (Manager3D.Is3D) return;
			MonitorTiles();
			UpdateCameraColor();
		}

		private void UpdateCameraColor()
		{
			if (cameraPositionColors == null || cameraPositionColors.Count == 0 ||
			    targetCamera == null || backgroundTiles == null)
				return;

			if (!cameraColorRenderersCached) CacheCameraColorRenderers();

			if (cameraColorRenderers.Length == 0) return;

			SpriteRenderer alphaSource = null;
			foreach (SpriteRenderer spriteRenderer in cameraColorRenderers)
			{
				if (spriteRenderer == null) continue;
				alphaSource = spriteRenderer;
				break;
			}

			if (alphaSource == null) return;

			Vector3 position = targetCamera.transform.position;
			if (cameraColorPositionPrecision > 0)
			{
				position.x = Mathf.Round(position.x / cameraColorPositionPrecision) * cameraColorPositionPrecision;
				position.y = Mathf.Round(position.y / cameraColorPositionPrecision) * cameraColorPositionPrecision;
			}

			float noise = Mathf.PerlinNoise(position.x * cameraColorWorldScale, position.y * cameraColorWorldScale);
			float colorPosition = noise * cameraPositionColors.Count;
			int firstColorIndex = Mathf.FloorToInt(colorPosition) % cameraPositionColors.Count;
			int secondColorIndex = (firstColorIndex + 1) % cameraPositionColors.Count;
			Color tint = Color.Lerp(cameraPositionColors[firstColorIndex], cameraPositionColors[secondColorIndex], colorPosition - Mathf.Floor(colorPosition));

			tint.a = alphaSource.color.a;

			if (hasCameraColor && Mathf.Max(Mathf.Abs(tint.r - lastCameraColor.r), Mathf.Abs(tint.g - lastCameraColor.g), Mathf.Abs(tint.b - lastCameraColor.b)) < 0.01f)
			{
				return;
			}

			foreach (SpriteRenderer spriteRenderer in cameraColorRenderers)
			{
				if (spriteRenderer != null) spriteRenderer.color = tint;
			}

			lastCameraColor = tint;
			hasCameraColor = true;
		}

		private void CacheCameraColorRenderers()
		{
			var renderers = new List<SpriteRenderer>();
			foreach (ParallaxColumn column in backgroundTiles)
			foreach (ParallaxStars tile in column.rows)
			{
				if (tile == null) continue;

				var spriteRenderer = tile.GetComponent<SpriteRenderer>();
				if (spriteRenderer != null) renderers.Add(spriteRenderer);
			}

			cameraColorRenderers = renderers.ToArray();
			cameraColorRenderersCached = true;
		}

		private void MonitorTiles()
		{
			if (targetCamera == null) targetCamera = UnityEngine.Camera.main;

			if (targetCamera == null || backgroundTiles == null || backgroundTiles.Count == 0) return;

			transform.position = targetCamera.transform.position * moveMultiplier;

			if (targetCamera.transform.position.x - backgroundTiles[centerColumn].rows[centerRow].transform.position.x > tileBounds.x)
			{
				MoveTiles(Vector2.right);
			}

			if (targetCamera.transform.position.x - backgroundTiles[centerColumn].rows[centerRow].transform.position.x < -tileBounds.x)
			{
				MoveTiles(Vector2.left);
			}

			if (targetCamera.transform.position.y - backgroundTiles[centerColumn].rows[centerRow].transform.position.y > tileBounds.y)
			{
				MoveTiles(Vector2.up);
			}

			if (targetCamera.transform.position.y - backgroundTiles[centerColumn].rows[centerRow].transform.position.y < -tileBounds.y)
			{
				MoveTiles(Vector2.down);
			}
		}

		private void MoveTiles(Vector2 direction)
		{
			bool? UpdateX = null;
			if (direction == Vector2.up)
			{
				UpdateX = false;
				foreach (ParallaxColumn c in backgroundTiles)
				{
					ParallaxStars from = c.rows[^1];
					c.rows.Remove(from);
					c.rows.Insert(0, from);
				}
			}
			else if (direction == Vector2.down)
			{
				UpdateX = false;
				foreach (ParallaxColumn c in backgroundTiles)
				{
					ParallaxStars from = c.rows[0];
					c.rows.Remove(from);
					c.rows.Add(from);
				}
			}
			else if (direction == Vector2.right)
			{
				UpdateX = true;
				ParallaxColumn from = backgroundTiles[0];
				backgroundTiles.Remove(from);
				backgroundTiles.Add(from);
			}
			else if (direction == Vector2.left)
			{
				UpdateX = true;
				ParallaxColumn from = backgroundTiles[^1];
				backgroundTiles.Remove(from);
				backgroundTiles.Insert(0, from);
			}

			RealignTiles(UpdateX);
		}

		private void CheckSpaceBackgroundObjects()
		{
			foreach (ParallaxColumn c in backgroundTiles)
			{
				foreach (ParallaxStars s in c.rows)
				{
					s.CheckSpaceObjects();
				}
			}
		}
	}

	[Serializable]
	public class ParallaxColumn
	{
		public List<ParallaxStars> rows = new();
	}

	[Serializable]
	public class ParallaxLayerDefinition
	{
		public string name = "Parallax Layer";
		public ParallaxStars prefab;
		public int columns = 7;
		public int rows = 7;
		public Vector3 objectScale = Vector3.one;
		public float moveMultiplier = 1f;
		public float zOffset = -1f;
		public List<ParallaxStars> prePlaced = new();
		public List<Color> cameraPositionColors = new();
		public float cameraColorWorldScale = 0.002f;
		public float cameraColorPositionPrecision = 0.01f;
	}
}