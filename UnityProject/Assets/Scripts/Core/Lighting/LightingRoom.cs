using System.Collections.Generic;
using Logs;
using UnityEngine;
using Objects.Lighting;

namespace Core.Lighting
{
	public class LightingRoom : MonoBehaviour
	{
		public LightingTileManager TileManager { get; set; }
		public LightingSourceManager SourceManager { get; set; }

		/// <summary>
		///     x = left
		///     y = top
		///     z = right
		///     w = down
		/// </summary>
		/// <value>The bounds of the LightTile Group in the room.</value>
		public Vector4 Bounds
		{
			get
			{
				if (TileManager != null)
				{
					return TileManager.bounds;
				}
				return Vector4.zero;
			}
		}

		private void Awake()
		{
			foreach (Transform child in transform)
			{
				if (child.gameObject.name == "Tiles")
				{
					child.gameObject.SetActive(true);
					TileManager = child.gameObject.GetComponent<LightingTileManager>();
				}
			}
			SourceManager = GetComponentInChildren<LightingSourceManager>();
		}

		private void Start()
		{
			Invoke(nameof(PrintBounds), 1f);
		}

		private void PrintBounds()
		{
			Loggy.Log("LIGHTING: Bounds calc for " + gameObject.name + ": " + Bounds, Category.Lighting);
		}

		public void LightSwitchOff()
		{
			foreach (KeyValuePair<Vector2, LightSource> light in SourceManager.lights)
			{
				light.Value.Trigger(false);
			}
		}

		public void LightSwitchOn()
		{
			foreach (KeyValuePair<Vector2, LightSource> light in SourceManager.lights)
			{
				light.Value.Trigger(true);
			}
		}
	}
}
