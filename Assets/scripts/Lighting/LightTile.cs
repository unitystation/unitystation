using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lighting
{
	public class LightTile : MonoBehaviour
	{

		public SpriteRenderer thisSprite{get; set;}

		void Awake()
		{
			thisSprite = GetComponentInChildren<SpriteRenderer>();
		}

		//If this tile touches a light source then absorb the light and send to surrounds
		public void InjectLight(float brightness){
			float leftOverLight = brightness - 10f;

			if (leftOverLight >= 0f) {
				var tempColor = thisSprite.color;
				tempColor.a = 0f;
				thisSprite.color = tempColor;
			}
		}
	}
}