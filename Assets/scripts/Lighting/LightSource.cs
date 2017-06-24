using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using InputControl;
using Events;
using UnityEngine.Events;
using Sprites;

namespace Lighting
{
	enum LightState
	{
		On,
		Off,
		Broken
	}

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
		/// The actual light effect that the light source represents
		/// </summary>
		public GameObject Light;

		/// <summary>
		/// The sprite to show when this light is turned on
		/// </summary>
		public Sprite SpriteLightOn;

		/// <summary>
		/// The sprite to show when this light is turned off
		/// </summary>
		public Sprite SpriteLightOff;

		void Awake()
		{
			Renderer = GetComponentInChildren<SpriteRenderer>();
		}

		void Start()
		{
			InitLightSprites();
		}

		public override void Trigger(bool state)
		{
			Renderer.sprite = state ? SpriteLightOn : SpriteLightOff;
			if (Light != null) {
				Light.SetActive(state);
			}
		}

		private void InitLightSprites()
		{
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