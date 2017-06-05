using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using InputControl;
using Events;
using UnityEngine.Events;
using Sprites;

namespace Lighting
{
	enum LightState {
		On,
		Off,
		Broken
	};

	public class LightSource : ObjectTrigger
	{
		/// <summary>
		/// The SpriteRenderer for this light
		/// </summary>
		private SpriteRenderer Renderer;
		/// <summary>
		/// The state of this light
		/// </summary>
		private LightState LightState;
		/// <summary>
		/// The sprite to show when this light is turned on
		/// </summary>
		public Sprite SpriteLightOn;
		/// <summary>
		/// The sprite to show when this light is turned off
		/// </summary>
		public Sprite SpriteLightOff;
		/// <summary>
		/// The the light gameobject child of the light
		/// </summary>
		public Light Light;

		void Awake()
		{
			Renderer = GetComponentInChildren<SpriteRenderer>();
			Light = GetComponentInChildren<Light>();
		}

		void Start(){
			InitLightSprites();
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
			Renderer.sprite = SpriteLightOn;
		}

		public void TurnOffLight(){
			Renderer.sprite = SpriteLightOff;
		}

		private void InitLightSprites() {
			LightState = LightState.On;

			//set the ON sprite to whatever the spriterenderer child has?
			var lightSprites = SpriteManager.LightSprites["lights"];
			SpriteLightOn = Renderer.sprite;

			//find the OFF light?
			string[] split = SpriteLightOn.name.Split('_');
			int onPos; 
			int.TryParse(split[1], out onPos);
			SpriteLightOff = lightSprites[onPos + 4];
		}
	}
}