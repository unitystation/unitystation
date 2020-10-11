using System.Collections;
using UnityEngine;

namespace Objects
{
	public class MonitorCamera : MonoBehaviour
	{
		private int baseSprite = 2;
		private SpriteRenderer spriteRenderer = null;

		private Sprite[] sprites = null;
		public float time = 0.3f;

		//private void Start()
		//{
		//	spriteRenderer = GetComponentInChildren<SpriteRenderer>();
		//	sprites = SpriteManager.MonitorSprites["monitors"];
		//	int.TryParse(spriteRenderer.sprite.name, out baseSprite);
		//	StartCoroutine(Animate());
		//}

		private IEnumerator Animate()
		{
			spriteRenderer.sprite = sprites[baseSprite];

			while (enabled)
			{
				for (int i = 0; i < 7; i++)
				{
					yield return WaitFor.Seconds(time);
					spriteRenderer.sprite = sprites[baseSprite + i * 8];
				}

				for (int i = 6; i >= 0; i--)
				{
					yield return WaitFor.Seconds(time);
					spriteRenderer.sprite = sprites[baseSprite + i * 8];
				}
			}
		}
	}
}
