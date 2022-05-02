using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;

namespace Tests
{
	public static class Utils
	{

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

		public static bool TryGetScriptableObjectGUID(Type scriptableObjectType, StringBuilder sb, out string assetGUID)
		{
			assetGUID = string.Empty;
			var typeName = scriptableObjectType.Name;
			string[] assets = AssetDatabase.FindAssets($"t: {typeName}");

			if (assets.Any() == false)
			{
				sb.AppendLine($"{typeName}: could not locate {typeName}");
				return false;
			}

			if (assets.Length > 1)
			{
				sb.AppendLine($"{typeName}: more than one {typeName} exists!");
				return false;
			}

			assetGUID = assets.First();
			return true;
		}
	}
}
