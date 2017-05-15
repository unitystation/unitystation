using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using InputControl;
using Events;
using UnityEngine.Events;
using Sprites;

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
		private List<Shroud> LocalShrouds = new List<Shroud>();

		private Sprite[] lightSprites;
		private bool updating = false;

		void Awake()
		{
			Renderer = GetComponentInChildren<SpriteRenderer>();
		}

		void Start(){
			LightOn = true;
			CamOcclusion = Camera.main.GetComponent<CameraOcclusion>();
			LightUpdate();
			lightSprites = SpriteManager.LightSprites["lights"];
			SpriteLightOn = Renderer.sprite;
			string[] split = SpriteLightOn.name.Split('_');
			int onPos; 
			int.TryParse(split[1], out onPos);
			SpriteLightOff = lightSprites[onPos + 4];
		}

		void OnEnable(){
			EventManager.LightUpdate += LightUpdate;
		}

		void OnDisable(){
			EventManager.LightUpdate -= LightUpdate;
		}

		private void LightUpdate(){
			if (!Renderer.isVisible)
				return;
			if (!updating) {
				updating = true;
				StartCoroutine(UpdateLight());
			}
		}

		IEnumerator UpdateLight(){
			
			yield return new WaitForSeconds(Random.Range(0.01f,0.1f));
				LocalShrouds = CamOcclusion.GetShroudsInDistanceOfPoint(MaxRange, this.transform.position);

				foreach (Shroud shroud in LocalShrouds) {
					//on changing light add all local lights then updat
					shroud.AddNewLightSource(this);
					shroud.UpdateLightSources();
				}
			updating = false;
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
				foreach (Shroud shroud in LocalShrouds) {
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
				foreach (Shroud shroud in LocalShrouds) {
					shroud.UpdateLightSources();
				}
				CamOcclusion.UpdateShroud();
			}
		}
	}
}