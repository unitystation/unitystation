using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lighting
{
	public class LightSwitch : MonoBehaviour
	{
		public bool isOn = true;
		private SpriteRenderer thisSprite;

		void Awake()
		{
			thisSprite = GetComponentInChildren<SpriteRenderer>();
		}

	}
}
