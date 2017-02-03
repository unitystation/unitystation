using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lighting
{
	public class LightSource : MonoBehaviour
	{

		private SpriteRenderer thisSprite;
		private LightTile contactLightTile;
		private bool sentLight = false;
	
		public float brightness = 80f;

		void Awake()
		{
			thisSprite = GetComponentInChildren<SpriteRenderer>();
		}

		void OnTriggerEnter2D(Collider2D coll)
		{
			if (coll.gameObject.layer == 13 && !sentLight) {
				sentLight = true;
				Debug.Log(gameObject + " HIT LIGHT TILE");
				LightTile tempLit = coll.gameObject.GetComponent <LightTile>();
				if (tempLit != null) {
					contactLightTile = tempLit;
					//Inject the brightness into the lighttile
					contactLightTile.InjectLight(brightness);
				}
			}
		}
	}
}