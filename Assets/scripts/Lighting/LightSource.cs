﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lighting
{
    public class LightSource : MonoBehaviour
	{
		/// <summary>
		/// The SpriteRenderer for this light
		/// </summary>
		private SpriteRenderer Renderer;
		/// <summary>
		/// The Maximum distance this light can cover in tiles
		/// </summary>
		[Header("Max distance in tiles")]
		public int MaxRange;
		/// <summary>
		/// The sprite to show when this light is turned on
		/// </summary>
		public Sprite SpriteLightOn;
		/// <summary>
		/// The sprite to show when this light is turned off
		/// </summary>
		public Sprite SpriteLightOff;
		/// <summary>
		/// The CameraOcclusion script with which we et shroud tiles
		/// </summary>
		private CameraOcclusion CamOcclusion;
		/// <summary>
		/// The local shrouds around this light
		/// </summary>
		private List<GameObject> LocalShrouds = new List<GameObject>();
		/// <summary>
		/// The componants of local shrouds around this light
		/// </summary>
		private List<Shroud> LocalShroudComponants = new List<Shroud>();

		void Awake()
		{
			Renderer = GetComponentInChildren<SpriteRenderer>();
		}

		void Start(){
			CamOcclusion = Camera.main.GetComponent<CameraOcclusion>();
		}

		void Update(){
			LocalShrouds = CamOcclusion.GetShroudsInDistanceOfPoint (MaxRange, this.transform.position);

			foreach (GameObject gameObject in LocalShrouds) {
				var shroud = gameObject.GetComponent<Shroud>();
				shroud.AddNewLightSource(this);
			}
		}

		public void TurnOnLight(){
	
		}

		public void TurnOffLight(){
			
		}
	}
}