using System;
using System.Linq;
using System.Text;
using UnityEditor;

namespace Tests
{
	public static class Utils
	{
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
