using System.Linq;
using System.Text;
using Items;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Tests
{
	public class ComponentTests : MonoBehaviour
	{
		/// <summary>
		/// Find null item traits from prefabs
		/// </summary>
		[Test]
		public void ItemTraitTest()
		{
			bool isok = true;
			var report = new StringBuilder();
			var prefabGUIDs = AssetDatabase.FindAssets("t:prefab", new string[] {"Assets/Prefabs"});
			var prefabPaths = prefabGUIDs.Select(AssetDatabase.GUIDToAssetPath);

			foreach (var prefab in prefabPaths)
			{
				var gameObject = AssetDatabase.LoadAssetAtPath<GameObject>(prefab);

				if(gameObject == null) continue;

				if (gameObject.TryGetComponent<ItemAttributesV2>(out var itemAttributesV2) == false) continue;

				if(itemAttributesV2.InitialTraits.Contains(null) == false) continue;
				
				if(gameObject.name.Contains("Base")) continue;

				report.AppendLine($"{prefab}: {gameObject.name} has null item trait. Remove empty index from list or add an item trait to index");
				isok = false;
			}

			if (isok == false)
			{
				Assert.Fail(report.ToString());
			}
		}
	}
}
