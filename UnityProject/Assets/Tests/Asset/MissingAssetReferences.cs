using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;

namespace Tests
{
	public class MissingAssetReferences 
	{
		[Test]
		public void TestAllChannelsTag()
		{
			List<string> listResult = new List<string>();
			string[] allPrefabs = SearchAndDestroy.GetAllPrefabs();
			foreach (string prefab in allPrefabs)
			{
				Object o = AssetDatabase.LoadMainAssetAtPath(prefab);
				GameObject go;
				try
				{
					go = (GameObject)o;
					Component[] components = go.GetComponentsInChildren<Component>(true);
					foreach (Component c in components)
					{
						if (c == null)
						{
							listResult.Add(prefab);
						}
					}
				}
				catch
				{
					Logger.LogFormat("For some reason, prefab {0} won't cast to GameObject", Category.UI, prefab);
				}
			}

			foreach (string s in listResult)
			{
				Debug.LogFormat("Missing reference found in prefab {0}", s);
			}

			Assert.IsEmpty(listResult, "Missing references found: {0}", string.Join(", ", listResult));
		}
	}
}