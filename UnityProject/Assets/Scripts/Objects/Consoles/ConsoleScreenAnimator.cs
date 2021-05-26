using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Systems.Electricity;

namespace Objects
{
	public class ConsoleScreenAnimator : MonoBehaviour, IAPCPowerable
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

		private void ToggleOn(bool turnOn)
		{
			if (turnOn)
			{
				if (SpriteHandlerHere == null)
				{
					Logger.Log($"{nameof(SpriteHandler)} is missing on {gameObject}.", Category.Sprites);
					return;
				}
				SpriteHandlerHere.PushTexture();
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

		#region IAPCPowerable

		public void PowerNetworkUpdate(float voltage) { }

		public void StateUpdate(PowerState state)
		{
			if (state == PowerState.Off || state == PowerState.LowVoltage)
			{
				ToggleOn(false);
			}
			else
			{
				ToggleOn(true);
			}
		}

		#endregion
	}
}
