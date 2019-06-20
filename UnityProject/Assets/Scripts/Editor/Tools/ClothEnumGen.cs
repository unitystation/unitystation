using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

#endif


public class ClothEnumGen : MonoBehaviour
{
#if UNITY_EDITOR


	[MenuItem("Tools/Generate Cloth Enum")]
	private static void GenerateClothEnum()
	{
		Dictionary<string, string> hierName = prepareObjects();
		// the path we want to write to
		string path = string.Concat(Application.dataPath, Path.DirectorySeparatorChar, "scripts", Path.DirectorySeparatorChar, "Items",
			Path.DirectorySeparatorChar, "ClothEnum.cs");
		if (File.Exists(path))
		{
			File.Delete(path);
		}

		try
		{
			// opens the file if it allready exists, creates it otherwise
			using (FileStream stream = File.Open(path, FileMode.OpenOrCreate, FileAccess.Write))
			{
				using (StreamWriter writer = new StreamWriter(stream))
				{
					StringBuilder sb = new StringBuilder();
					sb.AppendLine("// ----- AUTO GENERATED CODE ----- //");
					sb.AppendLine("using System.ComponentModel;");
					sb.AppendLine("public enum ClothEnum");
					sb.AppendLine("{");

					sb.AppendLine("\t[Description(\"\")]");
					sb.AppendLine("\tnone,");

					foreach (string hier in hierName.Keys)
					{
						sb.AppendLine("\t");
						sb.AppendLine(string.Format("\t[Description(\"{0}\")]", hier));
						sb.AppendLine(string.Format("\t{0},", hierName[hier]));
					}

					sb.AppendLine("}");
					writer.Write(sb.ToString());
				}
			}
			Logger.LogFormat("Wrote file to {0}", Category.ItemSpawn, path);
		}
		catch (Exception e)
		{
			Logger.LogErrorFormat("An error occured creating a clothing file: {0}", Category.PlayerSprites, e.Message);

			// if we have an error, it is certainly that the file is screwed up. Delete to be safe
			if (File.Exists(path))
			{
				File.Delete(path);
			}
		}

		AssetDatabase.Refresh();
	}

	private static Dictionary<string, string> prepareObjects()
	{
		Dictionary<string, string> tmpDic = new Dictionary<string, string>();
		DmObjectData dm = Resources.Load("DmObjectData") as DmObjectData;
		foreach (Dictionary<string, string> dic in dm.ObjectList)
		{
			string hier = ItemAttributes.TryGetAttr(dic, "hierarchy");
			if (!hier.Equals("") && hier.StartsWith("/obj/item/clothing/")) // these might require fine-tunung
			{
				//                                var name = ItemAttributes.tryGetAttr(dic, "name").Trim()
				//                                        .Replace('-', '_').Replace("\'","")
				//                                        .Replace(' ', '_').Replace("\\","")
				//                                        .Replace("`", "").Replace(".","")
				//                                        .Replace("(", "").Replace("!","")
				//                                        .ToLower();
				string[] hierz = hier.Split('/');
				string name = Regex.Replace(ItemAttributes.TryGetAttr(dic, "name").Trim().Replace('-', '_').Replace(' ', '_'), @"[^a-zA-Z0-9_]", "") + "__" +
				              hierz[hierz.GetUpperBound(0) - 2] + "_" + hierz[hierz.GetUpperBound(0) - 1] + "_" + hierz[hierz.GetUpperBound(0)];

				tmpDic.Add(hier, name);
			}
		}
		Logger.LogFormat("Prepared objects, tmpDic.size={0}", Category.ItemSpawn, tmpDic.Count);
		return tmpDic;
	}
#endif
}