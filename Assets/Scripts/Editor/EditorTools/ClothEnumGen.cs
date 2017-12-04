using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    static void GenerateClothEnum()
    {
        Dictionary<string, string> hierName = prepareObjects();
        // the path we want to write to
        string path = string.Concat(Application.dataPath, Path.DirectorySeparatorChar,
                "scripts", Path.DirectorySeparatorChar,
                "Items", Path.DirectorySeparatorChar,
                "ClothEnum.cs");
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
            Debug.LogFormat("Wrote file to {0}", path);
        }
        catch (Exception e)
        {
            Debug.LogException(e);

            // if we have an error, it is certainly that the file is screwed up. Delete to be save
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        AssetDatabase.Refresh();
    }

    static Dictionary<string, string> prepareObjects()
    {
        var tmpDic = new Dictionary<string, string>();
        var dm = Resources.Load("DmObjectData") as DmObjectData;
        foreach (var dic in dm.ObjectList)
        {
            var hier = ItemAttributes.tryGetAttr(dic, "hierarchy");
            if (!hier.Equals("") && hier.StartsWith("/obj/item/clothing/")) // these might require fine-tunung
            {
                //                                var name = ItemAttributes.tryGetAttr(dic, "name").Trim()
                //                                        .Replace('-', '_').Replace("\'","")
                //                                        .Replace(' ', '_').Replace("\\","")
                //                                        .Replace("`", "").Replace(".","")
                //                                        .Replace("(", "").Replace("!","")
                //                                        .ToLower();
                var hierz = hier.Split('/');
                var name = Regex.Replace(
                        ItemAttributes.tryGetAttr(dic, "name")
                                .Trim().Replace('-', '_').Replace(' ', '_')
                        , @"[^a-zA-Z0-9_]", "")
                           + "__" + hierz[hierz.GetUpperBound(0) - 2]
                           + "_" + hierz[hierz.GetUpperBound(0) - 1]
                           + "_" + hierz[hierz.GetUpperBound(0)]
                        ;

                tmpDic.Add(
                        hier,
                        name
                );
            }
        }
        Debug.LogFormat("Prepared objects, tmpDic.size={0}", tmpDic.Count);
        return tmpDic;
    }
#endif
}
