﻿using System.Collections.Generic;
using UnityEngine;

namespace Lighting
{
	public class LightingSourceManager : MonoBehaviour
	{
		private LightingRoom lightingRoomParent;
		public Dictionary<Vector2, LightSource> lights = new Dictionary<Vector2, LightSource>();

		private void Awake()
		{
			lightingRoomParent = GetComponentInParent<LightingRoom>();
		}

		private void Start()
		{
			LoadAllLights();
		}

		private void LoadAllLights()
		{
			foreach (Transform child in transform)
			{
				LightSource source = child.gameObject.GetComponent<LightSource>();
				if (source != null)
				{
					lights.Add(child.transform.position, source);
				}
				else
				{
					Logger.LogError("No LightSource component found!",Categories.Lighting);
				}
			}
		}

		public void UpdateRoomBrightness(LightSource theSource)
		{
		}
	}
}