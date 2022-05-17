using NUnit.Framework;
using UnityEditor;

namespace Tests.Asset
{
	[Category(nameof(Asset))]
	public class SpriteCatalogueTests
	{
		[Test]
		public void SpriteDataSOsAreInSpriteCatalogue()
		{
			var report = new TestReport();
			var spriteCatalogue = SpriteCatalogue.Instance;

			foreach (var asset in Utils.FindAssetsByType<SpriteDataSO>())
			{
				if (spriteCatalogue.Catalogue.Contains(asset) == false)
				{
					report.Fail()
						.AppendLine($"{asset.name} is not in {nameof(SpriteCatalogue)}.")
						.AppendLine($"{asset.name} has been added automatically and the change needs to be committed.");
					spriteCatalogue.Catalogue.Add(asset);
					EditorUtility.SetDirty(asset);
				}

				foreach (var (variant, i) in asset.Variance.WithIndex())
				{
					foreach (var (frame, j) in variant.Frames.WithIndex())
					{
						report.FailIf(frame.sprite, Is.Null)
							.AppendLine($"{asset.name}: Variant at index {i} has missing frame at index {j}.")
							.AppendLine("This will need to be fixed manually.");
					}
				}

				report.AppendLine();
			}

			EditorUtility.SetDirty(spriteCatalogue);
			AssetDatabase.SaveAssets();

			report.AssertPassed();
		}
	}
}