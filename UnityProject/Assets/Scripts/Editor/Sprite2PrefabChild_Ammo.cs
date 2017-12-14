using System.Linq;
using Tilemaps.Scripts.Behaviours.Objects;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

public class Sprite2PrefabChild_Ammo_MenuItem
{
    /// <summary>
    ///     Creates prefab, with sprites as child
    /// </summary>
    [MenuItem("Assets/Create/Sprite2PrefabChild/Ammo", false, 11)]
    public static void ScriptableObjectTemplateMenuItem()
    {
        var makeSeperateFolders = EditorUtility.DisplayDialog("Prefab Folders?",
            "Do you want each prefab in it's own folder?", "Yes", "No");
        for (var i = 0; i < Selection.objects.Length; i++)
        {
            var spriteSheet = AssetDatabase.GetAssetPath(Selection.objects[i]);
            var sprites = AssetDatabase.LoadAllAssetsAtPath(spriteSheet).OfType<Sprite>().ToArray();
            var splitSpriteSheet = spriteSheet.Split('/');
            var fullFolderPath = Inset(spriteSheet, 0, splitSpriteSheet[splitSpriteSheet.Length - 1].Length + 1) +
                                 "/" + Selection.objects[i].name;
            var folderName = Selection.objects[i].name;
            var adjFolderPath = InsetFromEnd(fullFolderPath, Selection.objects[i].name.Length + 1);

            if (!AssetDatabase.IsValidFolder(fullFolderPath))
            {
                AssetDatabase.CreateFolder(adjFolderPath, folderName);
            }

            var parent = new GameObject();
            var boxCollider = parent.AddComponent<BoxCollider2D>();
            var networkIdentiy = parent.AddComponent<NetworkIdentity>();
            var networkTransform = parent.AddComponent<NetworkTransform>();
            var itemAttributes = parent.AddComponent<ItemAttributes>();
            var magazineBehaviour = parent.AddComponent<MagazineBehaviour>();
            var objectBehaviour = parent.AddComponent<ObjectBehaviour>();
            var registerItem = parent.AddComponent<RegisterItem>();
            var spriteObject = new GameObject();
            var spriteRenderer = spriteObject.AddComponent<SpriteRenderer>();
            var spriteMaterial = Resources.Load("Sprite-PixelSnap", typeof(Material)) as Material;
            for (var j = 0; j < sprites.Length; j++)
            {
                EditorUtility.DisplayProgressBar(
                    i + 1 + "/" + Selection.objects.Length + " Generating Prefabs", "Prefab: " + j,
                    j / (float) sprites.Length);
                parent.name = sprites[j].name;
                spriteObject.name = "Sprite";
                spriteRenderer.sprite = sprites[j];
                spriteObject.GetComponent<SpriteRenderer>().material = spriteMaterial;


                var savePath = makeSeperateFolders
                    ? fullFolderPath + "/" + sprites[j].name + "/" + sprites[j].name + ".prefab"
                    : fullFolderPath + "/" + sprites[j].name + ".prefab";

                if (makeSeperateFolders)
                {
                    if (!AssetDatabase.IsValidFolder(fullFolderPath + "/" + sprites[j].name))
                    {
                        AssetDatabase.CreateFolder(fullFolderPath, sprites[j].name);
                    }
                }
                spriteObject.transform.parent = parent.transform;
                PrefabUtility.CreatePrefab(savePath, parent);
            }
            Object.DestroyImmediate(parent);
            Object.DestroyImmediate(spriteObject);
        }
        EditorUtility.ClearProgressBar();
    }

    /// <summary>
    ///     removes inset amounts from string ie. "0example01" with leftIn at 1 and with rightIn at 2 would result in "example"
    /// </summary>
    /// <param name="me"></param>
    /// <param name="inset"></param>
    /// <returns></returns>
    public static string Inset(string me, int leftIn, int rightIn)
    {
        return me.Substring(leftIn, me.Length - rightIn - leftIn);
    }

    /// <summary>
    ///     removes inset amount from string end ie. "example01" with inset at 2 would result in "example"
    /// </summary>
    /// <param name="me"></param>
    /// <param name="inset"></param>
    /// <returns></returns>
    public static string InsetFromEnd(string me, int inset)
    {
        return me.Substring(0, me.Length - inset);
    }
}