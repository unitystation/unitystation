using System;
using System.Collections;
using UnityEngine;

namespace Core.Lighting
{
	public class HighlightScan : MonoBehaviour
	{
		private SpriteRenderer spriteRenderer;

		private void Awake()
		{
			if (CustomNetworkManager.IsHeadless)
			{
				Logger.LogError("Highlight scanner detected on headless server! Despawning self..");
				Despawn.ClientSingle(this.gameObject);
				return;
			}

			spriteRenderer = GetComponentInChildren<SpriteRenderer>();
			Color alphaZero = spriteRenderer.color;
			alphaZero.a = 0;
			spriteRenderer.color = alphaZero;

			HighlightScanManager.Instance.HighlightScans.Add(this);
		}

		private void OnDisable()
		{
			HighlightScanManager.Instance.OrNull()?.HighlightScans.Remove(this);
		}

		public void Setup(Sprite sprite)
		{
			spriteRenderer.sprite = sprite;
		}

		public IEnumerator Highlight()
		{
			Color noAlpha = spriteRenderer.color;
			Color alpha = spriteRenderer.color;
			alpha.a = 0.99f;
			spriteRenderer.color = alpha;
			while (spriteRenderer.color.a > 0.1f)
			{
				yield return WaitFor.EndOfFrame;
				alpha = Color.Lerp(alpha, noAlpha, 0.5f * Time.deltaTime);
				spriteRenderer.color = alpha;
			}

			alpha.a = 0;
			spriteRenderer.color = alpha;
		}
	}
}