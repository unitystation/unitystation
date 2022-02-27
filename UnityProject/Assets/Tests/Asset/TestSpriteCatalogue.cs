using System.Collections;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Tests.Asset
{
	public class TestSpriteCatalogue
	{
		[Test]
		public void TestCatalogue()
		{
			var report = new StringBuilder();
			bool Failed = false;
			bool SuperFailed = false;

			List<SpriteDataSO> ForceUpdateList = new List<SpriteDataSO>();

			var Assets = FindAssetsByType<SpriteDataSO>();
			if (SpriteCatalogue.Instance == null)
			{
				Resources.LoadAll<SpriteCatalogue>("ScriptableObjects/SOs singletons");
			}

			foreach (var Assete in Assets)
			{
				if (SpriteCatalogue.Instance.Catalogue.Contains(Assete) == false)
				{
					report.AppendLine($"{Assete.name}: Not in Catalogue");
					ForceUpdateList.Add(Assete);
					Failed = true;
				}

				int i = 0;
				foreach (var variant in Assete.Variance)
				{
					int j = 0;
					foreach (var Frame in variant.Frames)
					{
						if (Frame.sprite == null)
						{
							report.AppendLine($"{Assete.name}: Has missing sprite frame at {i} Variant at {j} Frame");
							Failed = true;
							SuperFailed = true;
						}
						j++;
					}

					i++;
				}
			}

			foreach (var ToForceUpdate in ForceUpdateList)
			{
				SpriteCatalogue.Instance.Catalogue.Add(ToForceUpdate);
				EditorUtility.SetDirty(ToForceUpdate);
			}

			EditorUtility.SetDirty(SpriteCatalogue.Instance);
			AssetDatabase.SaveAssets();

			if (Failed)
			{

				if (SuperFailed == false)
				{
					report.AppendLine(
						"hey, This is been handled automatically you just need to now Commit the changes");
				}
				else
				{
					report.AppendLine(
						"hey, It's **not** been handled automatically and requires manual intervention on some files");
				}
				Assert.Fail(report.ToString());
			}
		}

		public static List<T> FindAssetsByType<T>() where T : UnityEngine.Object
		{
			List<T> assets = new List<T>();
			string[] guids = AssetDatabase.FindAssets(string.Format("t:{0}", typeof(T)));
			for (int i = 0; i < guids.Length; i++)
			{
				string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
				T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
				if (asset != null)
				{
					assets.Add(asset);
				}
			}

			return assets;
		}
	}
}