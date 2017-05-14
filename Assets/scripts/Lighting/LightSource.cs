using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using InputControl;

namespace Lighting
{
	public class LightSource : ObjectTrigger
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
		/// The state of this light
		/// </summary>
		public bool LightOn;
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

		void Awake()
		{
			Renderer = GetComponentInChildren<SpriteRenderer>();
		}

		void Start(){
			LightOn = true;
			CamOcclusion = Camera.main.GetComponent<CameraOcclusion>();
		}

		void Update(){
			LocalShrouds = CamOcclusion.GetShroudsInDistanceOfPoint(MaxRange, this.transform.position);

			foreach (GameObject gameObject in LocalShrouds) {
				var shroud = gameObject.GetComponent<Shroud>();
				//on changing light add all local lights then updat
				shroud.AddNewLightSource(this);
				shroud.UpdateLightSources ();
			}
		}

		public override void Trigger(bool state){
			//turn lights off
			if (!state) {
				TurnOffLight();
			} 
			//turn on
			else {
				TurnOnLight();
			}
		}

		public void TurnOnLight(){
			LightOn = true;
			Renderer.sprite = SpriteLightOn;
			if (CamOcclusion != null) {
				foreach (GameObject gameObject in LocalShrouds) {
					var shroud = gameObject.GetComponent<Shroud>();
					//on changing light add all local lights then updat
					shroud.AddNewLightSource(this);
					shroud.UpdateLightSources ();
				}
				CamOcclusion.UpdateShroud();
			}
		}

		public void TurnOffLight(){
			LightOn = false;
			Renderer.sprite = SpriteLightOff;
			if (CamOcclusion != null) {
				foreach (GameObject gameObject in LocalShrouds) {
					var shroud = gameObject.GetComponent<Shroud>();
					shroud.UpdateLightSources();
				}
				CamOcclusion.UpdateShroud();
			}
		}
	}
}