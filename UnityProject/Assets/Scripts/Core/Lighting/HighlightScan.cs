using System;
using System.Collections;
using UnityEngine;

namespace Core.Lighting
{
	public class HighlightScan : MonoBehaviour
	{
		private SpriteRenderer renderer;

		private void Awake()
		{
			if (CustomNetworkManager.IsHeadless)
			{
				Logger.LogError("Highlight scanner detected on headless server! Despawning self..");
				Despawn.ClientSingle(this.gameObject);
				return;
			}

			renderer = GetComponentInChildren<SpriteRenderer>();
			Color alphaZero = renderer.color;
			alphaZero.a = 0;
			renderer.color = alphaZero;

			HighlightScanManager.Instance.HighlightScans.Add(this);
		}

		private void OnDisable()
		{
			HighlightScanManager.Instance.HighlightScans.Remove(this);
		}

		public void Setup(Sprite sprite)
		{
			renderer.sprite = sprite;
		}

		public IEnumerator Highlight()
		{
			Color noAlpha = renderer.color;
			Color alpha = renderer.color;
			alpha.a = 0.99f;
			renderer.color = alpha;
			while (renderer.color.a > 0.1f)
			{
				yield return WaitFor.EndOfFrame;
				alpha = Color.Lerp(alpha, noAlpha, 0.5f * Time.deltaTime);
				renderer.color = alpha;
			}

			alpha.a = 0;
			renderer.color = alpha;
		}
	}
}