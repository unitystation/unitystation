using UnityEngine;
using US13.Core.Input_System;

namespace US13.Core.Lighting
{
	[ExecuteInEditMode]
	public class AmbientTile : ObjectTrigger
	{
		public Color offColor = new Color32(0, 0, 0, 255);
		public Color onColor = new Color32(105, 105, 105, 255);
		private SpriteRenderer spriteRend;

		private void Start()
		{
			spriteRend = GetComponent<SpriteRenderer>();
			spriteRend.color = onColor;
		}

		//LightSource sends a message to this method
		public override void Trigger(bool iState)
		{
			if (spriteRend == null)
			{
				spriteRend = GetComponent<SpriteRenderer>();
			}

			spriteRend.color = iState ? onColor : offColor;
		}
	}
}