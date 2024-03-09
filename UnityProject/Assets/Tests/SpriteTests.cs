using System.Text;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Tests
{
	public class SpriteTests
	{
		[Test]
		public void SpriteRenderersDoNotHaveANullSpriteHandlerSpritesSet()
		{
			var report = new TestReport();

			foreach (var prefab in Utils.FindPrefabs(false))
			{
				foreach (Transform child in prefab.transform)
				{
					if (child.TryGetComponent<SpriteRenderer>(out var spriteRenderer) == false) continue;
					if (child.TryGetComponent<SpriteHandler>(out var spriteHandler) == false) continue;

					var spritesSetName = $"{nameof(SpriteHandler)}.{nameof(SpriteHandler.PresentSpritesSet)}";

					report.FailIf(spriteRenderer.sprite != null && spriteHandler.PresentSpritesSet == null)
						.Append($"{prefab.name}: The child \"{child.name}\" has a {nameof(SpriteRenderer)} but ")
						.Append($"{spritesSetName} is null, this will lead to an invisible object!")
						.Append("Set the PresentSpriteSet on the spriteHandler or remove the spriteRender sprite.")
						.AppendLine();
				}
			}

			report.AssertPassed();
		}
	}
}
