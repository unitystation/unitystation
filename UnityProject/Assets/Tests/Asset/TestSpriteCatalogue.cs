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
					Failed = true;
				}
			}

			Dictionary<int, SpriteDataSO> SpriteDataSOs = new Dictionary<int, SpriteDataSO>();

			foreach (var Assete in Assets)
			{
				if (SpriteDataSOs.ContainsKey(Assete.setID))
				{
					report.AppendLine($"{Assete.name}: Duplicated ID with {SpriteDataSOs[Assete.setID]}" );
					Failed = true;
				}

				SpriteDataSOs[Assete.setID] = Assete;
			}

			EditorUtility.SetDirty( SpriteCatalogue.Instance);
			AssetDatabase.SaveAssets();

			if (Failed)
			{
				Assert.Fail(report.ToString());
				return;
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