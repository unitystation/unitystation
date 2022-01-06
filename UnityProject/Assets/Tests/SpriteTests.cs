using System.Text;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Tests
{
	public class SpriteTests
	{
		[Test]
		public void SpriteHandlerTest()
		{
			var report = new StringBuilder();
			var failed = false;

			string[] allPrefabs = SearchAndDestroy.GetAllPrefabs();
			foreach (string prefab in allPrefabs)
			{
				Object o = AssetDatabase.LoadMainAssetAtPath(prefab);

				// Check if it's gameobject
				if (!(o is GameObject))
				{
					Logger.LogFormat("For some reason, prefab {0} won't cast to GameObject", Category.Tests, prefab);
					continue;
				}

				var gameObject = o as GameObject;

				foreach (Transform child in gameObject.transform)
				{
					if(child.TryGetComponent<SpriteRenderer>(out var spriteRenderer) == false) continue;
					if(child.TryGetComponent<SpriteHandler>(out var spriteHandler) == false) continue;

					if (spriteRenderer.sprite != null && spriteHandler.PresentSpritesSet == null)
					{
						report.AppendLine($"{gameObject.name} has a null spriteHandler PresentSpriteSet but a not null spriteRender sprite on {child.name}," +
						                  $" this will lead to an invisible object! Set the PresentSpriteSet on the spriteHandler or remove the spriteRender sprite.");
						failed = true;
					}
				}
			}

			if (failed)
			{
				Assert.Fail(report.ToString());
			}
		}
	}
}
