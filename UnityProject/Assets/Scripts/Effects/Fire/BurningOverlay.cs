using System;
using UnityEngine;

namespace Effects.Overlays
{
	/// <summary>
	/// Handles animation of burning overlay prefabs, which are injected into burning objects.
	/// </summary>
	[RequireComponent(typeof(SpriteHandler))]
	public class BurningOverlay : MonoBehaviour
	{
		private SpriteHandler spriteHandler;

		private void Awake()
		{
			spriteHandler = GetComponent<SpriteHandler>();
		}

		private void Start()
		{
			//wait until we are told to burn
			StopBurning();
		}

		/// <summary>
		/// start displaying the burning animation
		/// </summary>
		public void Burn()
		{
			if (spriteHandler == null)
			{
				spriteHandler = GetComponent<SpriteHandler>();
			}

			spriteHandler.ChangeSprite(0); // Load SO into SpriteRenderer
			spriteHandler.PushTexture();
		}

		/// <summary>
		/// stop the burning animation
		/// </summary>
		public void StopBurning()
		{
			spriteHandler.PushClear();
		}
	}
}
