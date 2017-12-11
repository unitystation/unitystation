using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Linq;
using Tilemaps.Scripts.Behaviours.Objects;
using UnityEngine.Networking;
using Weapons;


public class Sprite2PrefabChild_Ammo_MenuItem
{
    /// <summary>
    /// Creates prefab, with sprites as child
    /// </summary>
    [MenuItem("Assets/Create/Sprite2PrefabChild/Ammo", false, 11)]
    public static void ScriptableObjectTemplateMenuItem()
    {
        bool makeSeperateFolders = EditorUtility.DisplayDialog("Prefab Folders?", "Do you want each prefab in it's own folder?", "Yes", "No");
        for (int i = 0; i < Selection.objects.Length; i++)
        {
            string spriteSheet = AssetDatabase.GetAssetPath(Selection.objects[i]);
            Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(spriteSheet).OfType<Sprite>().ToArray();
            string[] splitSpriteSheet = spriteSheet.Split(new char[] { '/' });
            string fullFolderPath = Inset(spriteSheet, 0, splitSpriteSheet[splitSpriteSheet.Length - 1].Length + 1) + "/" + Selection.objects[i].name;
            string folderName = Selection.objects[i].name;
            string adjFolderPath = InsetFromEnd(fullFolderPath, Selection.objects[i].name.Length + 1);

            if (!AssetDatabase.IsValidFolder(fullFolderPath))
            {
                AssetDatabase.CreateFolder(adjFolderPath, folderName);
            }

            GameObject parent = new GameObject();
            BoxCollider2D boxCollider = parent.AddComponent<BoxCollider2D>();
			NetworkIdentity networkIdentiy = parent.AddComponent<NetworkIdentity>();
			NetworkTransform networkTransform = parent.AddComponent<NetworkTransform>();
			ItemAttributes itemAttributes = parent.AddComponent<ItemAttributes>();
            MagazineBehaviour magazineBehaviour = parent.AddComponent<MagazineBehaviour>();
			ObjectBehaviour objectBehaviour = parent.AddComponent<ObjectBehaviour>();
			RegisterItem registerItem = parent.AddComponent<RegisterItem>();
            GameObject spriteObject = new GameObject();
            SpriteRenderer spriteRenderer = spriteObject.AddComponent<SpriteRenderer>();
            Material spriteMaterial = Resources.Load("Sprite-PixelSnap", typeof(Material)) as Material;
            for (int j = 0; j < sprites.Length; j++)
            {
                EditorUtility.DisplayProgressBar((i + 1).ToString() + "/" + Selection.objects.Length + " Generating Prefabs", "Prefab: " + j, (float)j / (float)sprites.Length);
                parent.name = sprites[j].name;
                spriteObject.name = "Sprite";
                spriteRenderer.sprite = sprites[j];
                spriteObject.GetComponent<SpriteRenderer>().material = spriteMaterial;



                string savePath = makeSeperateFolders ? fullFolderPath + "/" + sprites[j].name + "/" + sprites[j].name + ".prefab" : fullFolderPath + "/" + sprites[j].name + ".prefab";

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
            GameObject.DestroyImmediate(parent);
            GameObject.DestroyImmediate(spriteObject);
        }
        EditorUtility.ClearProgressBar();

    }

    /// <summary>
    /// removes inset amounts from string ie. "0example01" with leftIn at 1 and with rightIn at 2 would result in "example"
    /// </summary>
    /// <param name="me"></param>
    /// <param name="inset"></param>
    /// <returns></returns>
    public static string Inset(string me, int leftIn, int rightIn)
    {
        return me.Substring(leftIn, me.Length - rightIn - leftIn);
    }

    /// <summary>
    /// removes inset amount from string end ie. "example01" with inset at 2 would result in "example"
    /// </summary>
    /// <param name="me"></param>
    /// <param name="inset"></param>
    /// <returns></returns>
    public static string InsetFromEnd(string me, int inset)
    {
        return me.Substring(0, me.Length - inset);
    }

}
