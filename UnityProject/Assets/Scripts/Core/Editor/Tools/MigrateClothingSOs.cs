/// <summary>
/// Converts old clothing sprite SO data to new SO. Restores references to SOs in ClothingV2.
/// 1. Generate the new SOs. Confirm they look good. Unless they are adjusted SOs, they will have a temporary name as the old SOs will still exist.
/// 2. Update Clothing V2 to use new SOs. Confirm they look good.
/// 3. Delete the old SOs - just do this in editor: search for t:BaseClothData and delete all results.
/// 4. Rename the temporarily named new SOs back to original SO name.
/// </summary>

/* 
 * If you wish to use this tool:
 * Uncomment all this.
 * Add `public BaseClothData currentClothData;` to ClothingV2 and set `allClothingData` public until you're done.
 * Acquire the original clothing SO scripts (and associated .meta files) if they're no longer part of the project.
 * Confirm ClothingV2 currentClothData is populated on the clothing you wish to convert.
 * Confirm the clothing SOs to be converted are also populated, and their disabled `Script` fields are not empty.

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Systems.Clothing;

namespace Core.Editor.Tools
{
	public class MigrateClothingSOs : MonoBehaviour
	{		
		const string tempSuffix = "-new321-"; // must be unique in the filepathname
		const string adjustedSuffix = "_adjusted";
		
		#region Generate New SOs

		static int countBelts = 0;
		static int countClothing = 0;
		static int countClothingAdjusted = 0;
		static int countContainers = 0;
		static int countHeadsets = 0;
		static int countTotal = 0;

		[MenuItem("Tools/Clothing Migration/Test Generate Path")]
		private static void TestGeneratePath()
		{
			Debug.Log(GeneratePath("madness.test", "[beforedot]"));
			Debug.Log(GeneratePath("madness.reallymad.test", "[beforedot]"));
		}

		[MenuItem("Tools/Clothing Migration/1 Generate New SOs")]
		private static void GenerateNewSOs()
		{
			countBelts = 0;
			countClothing = 0;
			countClothingAdjusted = 0;
			countContainers = 0;
			countHeadsets = 0;
			countTotal = 0;

			AssetDatabase.StartAssetEditing();
			foreach (BaseClothData cloth in GenerateSpriteSO.FindAssetsByType<BaseClothData>())
			{
				ConvertCloth(cloth);
				countTotal++;
			}

			Debug.Log($"Generated: Belts: {countBelts}. Clothing: {countClothing}. Clothing_adjusted: {countClothingAdjusted}. " +
				$"Containers: {countContainers}. Headsets: {countHeadsets}. Converted {countTotal} SOs in total.");

			AssetDatabase.StopAssetEditing();
			AssetDatabase.SaveAssets();
		}

		private static void ConvertCloth(BaseClothData cloth)
		{
			string path = AssetDatabase.GetAssetPath(cloth);
			ClothingDataV2 dataV2 = default;

			if (cloth is ClothingData clothing)
			{
				countClothing++;
				dataV2 = CreateNewDataForBase(clothing.Base);
				
				if (clothing.Base_Adjusted.SpriteEquipped != null ||
					clothing.Base_Adjusted.SpriteInHandsLeft != null ||
					clothing.Base_Adjusted.SpriteInHandsRight != null ||
					clothing.Base_Adjusted.SpriteItemIcon != null)
				{
					AssetDatabase.CreateAsset(CreateNewDataForAdjusted(clothing.Base, clothing.Base_Adjusted), GeneratePath(path, adjustedSuffix));
					countClothingAdjusted++;
				}
			}
			else if (cloth is BeltData belt)
			{
				dataV2 = CreateNewDataForBase(belt.sprites);
				countBelts++;
			}
			else if (cloth is ContainerData container)
			{
				dataV2 = CreateNewDataForBase(container.Sprites);
				countContainers++;
			}
			else if (cloth is HeadsetData headsetData)
			{
				dataV2 = CreateNewDataForBase(headsetData.Sprites);
				countHeadsets++;
			}

			AssetDatabase.CreateAsset(dataV2, GeneratePath(path, tempSuffix));
		}

		private static ClothingDataV2 CreateNewDataForBase(EquippedData data)
		{
			ClothingDataV2 dataV2 = ScriptableObject.CreateInstance<ClothingDataV2>();

			dataV2.SpriteEquipped = data.SpriteEquipped;
			dataV2.SpriteInHandsLeft = data.SpriteInHandsLeft;
			dataV2.SpriteInHandsRight = data.SpriteInHandsRight;
			dataV2.SpriteItemIcon = data.SpriteItemIcon;
			dataV2.Palette = data.Palette;
			dataV2.IsPaletted = data.IsPaletted;

			return dataV2;
		}

		private static ClothingDataV2 CreateNewDataForAdjusted(EquippedData baseData, EquippedData adjustedData)
		{
			ClothingDataV2 dataV2 = ScriptableObject.CreateInstance<ClothingDataV2>();

			dataV2.SpriteEquipped = adjustedData.SpriteEquipped == null ? baseData.SpriteEquipped : adjustedData.SpriteEquipped;
			dataV2.SpriteInHandsLeft = adjustedData.SpriteInHandsLeft == null ? baseData.SpriteInHandsLeft : adjustedData.SpriteInHandsLeft;
			dataV2.SpriteInHandsRight = adjustedData.SpriteInHandsRight == null ? baseData.SpriteInHandsRight : adjustedData.SpriteInHandsRight;
			dataV2.SpriteItemIcon = adjustedData.SpriteItemIcon == null ? baseData.SpriteItemIcon : adjustedData.SpriteItemIcon;
			dataV2.Palette = adjustedData.Palette;
			dataV2.IsPaletted = adjustedData.IsPaletted;
			
			return dataV2;
		}

		#endregion

		#region Update ClothingV2 component

		[MenuItem("Tools/Clothing Migration/2 Update ClothingV2 Component")]
		private static void UpdateClothingV2()
		{
			int baseCount = 1;
			int adjustedCount = 0;

			AssetDatabase.StartAssetEditing();
			foreach (ClothingV2 clothing in GetAllClothingV2())
			{
				if (clothing.currentClothData == null) continue;

				string oldClothPath = AssetDatabase.GetAssetPath(clothing.currentClothData);
				string baseClothPath = GeneratePath(oldClothPath, tempSuffix);
				ClothingDataV2 baseClothingData = AssetDatabase.LoadAssetAtPath<ClothingDataV2>(baseClothPath);
				if (baseClothingData != null)
				{
					clothing.allClothingData.Add(baseClothingData);
					baseCount++;
				}
				else
				{
					Debug.LogError($"New clothing data not found at {baseClothPath}!");
					continue;
				}

				string adjustedClothPath = baseClothPath.Replace(tempSuffix, adjustedSuffix);
				ClothingDataV2 adjustedClothingData = AssetDatabase.LoadAssetAtPath<ClothingDataV2>(adjustedClothPath);
				// If null, this is fine: probably means we didn't generate an SO for adjusted (original was empty)
				if (adjustedClothingData != null)
				{
					clothing.allClothingData.Add(adjustedClothingData);
					adjustedCount++;
				}

				PrefabUtility.SavePrefabAsset(clothing.gameObject);
			}

			AssetDatabase.StopAssetEditing();
			AssetDatabase.SaveAssets();

			Debug.Log($"Updated base data: {baseCount}. Adjusted: {adjustedCount}.");
		}

		private static IEnumerable<ClothingV2> GetAllClothingV2()
		{
			var guids = AssetDatabase.FindAssets("t:Prefab");
			foreach (var guid in guids)
			{
				var path = AssetDatabase.GUIDToAssetPath(guid);
				var toCheck = AssetDatabase.LoadAllAssetsAtPath(path);
				foreach (var obj in toCheck)
				{
					var go = obj as GameObject;
					if (go == null) continue;

					var comp = go.GetComponent<ClothingV2>();
					if (comp != null)
					{
						yield return comp;
					}
				}
			}
		}

		#endregion

		#region Rename New Base SOs

		[MenuItem("Tools/Clothing Migration/4 Rename New Base SOs")]
		private static void RenameSOTempNames()
		{
			int renameCount = 0;

			AssetDatabase.StartAssetEditing();
			var allCloths = GenerateSpriteSO.FindAssetsByType<ClothingDataV2>();
			foreach (var cloth in allCloths)
			{
				string clothPath = AssetDatabase.GetAssetPath(cloth);
				if (clothPath.Contains(tempSuffix) == false) continue;
				string filename = Path.GetFileNameWithoutExtension(clothPath);
				string newName = filename.Replace(tempSuffix, "");

				AssetDatabase.RenameAsset(clothPath, newName);
				renameCount++;
			}

			AssetDatabase.StopAssetEditing();
			AssetDatabase.SaveAssets();

			Debug.Log($"Renamed {renameCount} assets.");
		}

		#endregion

		private static string GeneratePath(string filepath, string suffix)
		{
			return Path.GetDirectoryName(filepath) + Path.GetFileNameWithoutExtension(filepath) + suffix + Path.GetExtension(filepath);
		}
	}
}
*/
