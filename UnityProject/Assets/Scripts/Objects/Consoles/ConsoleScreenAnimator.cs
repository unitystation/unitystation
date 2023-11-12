using System;
using System.Collections;
using System.Collections.Generic;
using Logs;
using UnityEngine;
using Systems.Electricity;
using Objects.Construction;

namespace Objects
{
	public class ConsoleScreenAnimator : MonoBehaviour
	{
		public SpriteHandler SpriteHandlerHere {
			get {
				if (spriteHandler == null)
				{
					spriteHandler = GetComponentInChildren<SpriteHandler>();
				}
				return spriteHandler;
			}
			set {
				spriteHandler = value;
			}
		}

		[SerializeField]
		private SpriteHandler spriteHandler;
		public GameObject ScreenGlow;

		public void ToggleOn(bool turnOn)
		{
			if (turnOn)
			{
				if (SpriteHandlerHere == null)
				{
					Loggy.Log($"{nameof(SpriteHandler)} is missing on {gameObject}.", Category.Sprites);
					return;
				}
				SpriteHandlerHere.PushTexture();
				ScreenGlow.SetActive(true);
			}
			else
			{
				SpriteHandlerHere.PushClear();
				if (ScreenGlow != null)
				{
					ScreenGlow.SetActive(false);
				}
			}
		}
	}
}
