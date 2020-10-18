using System;
using UnityEngine;

namespace Objects
{
	public class OutsideFlashingLight : MonoBehaviour
	{
		public float flashWaitTime = 1f;

		public GameObject lightSource;
		public SpriteRenderer lightSprite;
		public Color spriteOffCol;
		public Color spriteOnCol;

		private void OnEnable()
		{
			UpdateManager.Add(UpdateMe, flashWaitTime);
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
		}

		private void UpdateMe()
		{
			SwitchLights();
		}

		private void SwitchLights()
		{
			if (!lightSource.activeSelf)
			{
				lightSource.SetActive(true);
				lightSprite.color = spriteOnCol;
			}
			else
			{
				lightSource.SetActive(false);
				lightSprite.color = spriteOffCol;
			}
		}
	}
}
