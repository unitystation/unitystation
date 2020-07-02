using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Newtonsoft.Json;
using System.Linq;
using System.Reflection;

public class GenerateSpriteSO : EditorWindow
{

	[MenuItem("Tools/GenerateSpriteSO")]
	public static void Generate()
	{
		DirSearch_ex3(Application.dataPath);
	}

	public static void DirSearch_ex3(string sDir)
	{
		//Console.WriteLine("DirSearch..(" + sDir + ")");

			Logger.Log(sDir);

			var Files = Directory.GetFiles(sDir);
			foreach (string f in Files)
			{
				if (f.Contains(".png") && f.Contains(".meta") == false)
				{
					var path =  f;
					var TT = path.Replace(Application.dataPath,"Assets");
					var Sprites = AssetDatabase.LoadAllAssetsAtPath(TT).OfType<Sprite>().ToArray();
					if (Sprites.Length > 1)
					{
						Sprites = Sprites.OrderBy(x => int.Parse(x.name.Substring(x.name.LastIndexOf('_') + 1))).ToArray();
					}
					//yeah If you named your sub sprites rip, have to find another way of ordering them correctly since the editor doesnt want to do that		E
					var EquippedData = (TextAsset)AssetDatabase.LoadAssetAtPath(path.Replace(".png", ".json").Replace(Application.dataPath,"Assets"), typeof(TextAsset));
					var SpriteData = ScriptableObject.CreateInstance<SpriteDataSO>();


					//SpriteData.
					SpriteData = FilloutData(EquippedData,Sprites,  SpriteData);
					AssetDatabase.CreateAsset(SpriteData, f.Replace(".png",".asset").Replace(Application.dataPath,"Assets"));


					//Gizmos.DrawIcon();
					//DrawIcon(SpriteData,  Sprites[0].texture);
//https://forum.unity.com/threads/editor-changing-an-items-icon-in-the-project-window.272061/

				}
				Logger.Log(f);
			}

			/*foreach (string d in Directory.GetDirectories(sDir))
			{
				DirSearch_ex3(d);
			}*/

	}

	/*
	public static void DrawIcon(ScriptableObject gameObject, Texture texture)
	{


		var largeIcons = GetTextures("sv_label_", string.Empty, 0, 8);
		var icon = largeIcons[0];
		icon.image = texture;
		var egu = typeof(EditorGUIUtility);
		var flags = BindingFlags.InvokeMethod | BindingFlags.Static | BindingFlags.NonPublic;
		var args = new object[] { gameObject, icon.image };
		var setIcon = egu.GetMethod("SetIconForObject", flags, null, new System.Type[]{typeof(UnityEngine.Object), typeof(Texture2D)}, null);
		setIcon.Invoke(null, args);
	}
	public static  GUIContent[] GetTextures(string baseName, string postFix, int startIndex, int count)
	{
		GUIContent[] array = new GUIContent[count];
		for (int i = 0; i < count; i++)
		{
			array[i] = EditorGUIUtility.IconContent(baseName + (startIndex + i) + postFix);
		}
		return array;
	}
	*/


	//######################

	//Assets/Editor/ProjectIcons.cs
	/*using UnityEngine;
	using UnityEditor;
	using System.Collections;
 
	public class ProjectIcons : Editor {
		[MenuItem( "EDITORS/ProjectIcons/Enable" )]
		static void EnableIcons () {
			EditorApplication.projectWindowItemOnGUI -= ProjectIcons.MyCallback();
			EditorApplication.projectWindowItemOnGUI += ProjectIcons.MyCallback();
		}

		[MenuItem( "EDITORS/ProjectIcons/Disable" )]
		static void DisableIcons () {
			EditorApplication.projectWindowItemOnGUI -= ProjectIcons.MyCallback();
		}

		static EditorApplication.ProjectWindowItemCallback MyCallback () {
			EditorApplication.ProjectWindowItemCallback myCallback = new EditorApplication.ProjectWindowItemCallback( IconGUI );
			return myCallback;
		}

		static void IconGUI ( string s, Rect r ) {
			string fileName = AssetDatabase.GUIDToAssetPath( s );
			int index = fileName.LastIndexOf( '.' );
			if ( index == -1 ) return;
			string fileType = fileName.Substring( fileName.LastIndexOf( "." ) + 1 );
			r.width = r.height;
			switch ( fileType ) {
				case "cs":
					//Put your icon images somewhere in the project, and refer to them with a string here
					GUI.DrawTexture( r, (Texture2D) AssetDatabase.LoadAssetAtPath( "Assets/Editor/Icons/Icon1.psd", typeof( Texture2D ) ) );
					break;
				case "psd":
					GUI.DrawTexture( r, (Texture2D) AssetDatabase.LoadAssetAtPath( "Assets/Editor/Icons/Icon2.psd", typeof( Texture2D ) ) );
					break;
				case "png":
					GUI.DrawTexture( r, (Texture2D) AssetDatabase.LoadAssetAtPath( "Assets/Editor/Icons/Icon3.psd", typeof( Texture2D ) ) );
					break;
			}
		}
	}*/

	public static SpriteDataSO FilloutData(TextAsset EquippedData, Sprite[]  Sprites, SpriteDataSO SpriteData)
	{
		SpriteJson spriteJson = null;

		if (EquippedData != null) {
			spriteJson = JsonConvert.DeserializeObject<SpriteJson>(EquippedData.text);
		}
		else
		{
			if (Sprites.Length > 1)
			{
				Logger.LogError("OH NO json File wasn't found for " + Sprites[0].name, Category.Editor);
			}
			SpriteData.Variance.Add(new SpriteDataSO.Variant());
			SpriteData.Variance[0].Frames.Add(new SpriteDataSO.Frame());
			SpriteData.Variance[0].Frames[0].sprite = Sprites[0];
			return SpriteData;
		}
		int variance = 0;
		int frame = 0;
		for (int J = 0; J < spriteJson.Number_Of_Variants; J++)
		{
			SpriteData.Variance.Add(new SpriteDataSO.Variant());
		}

		foreach (var SP in Sprites)
		{
			var info = new SpriteDataSO.Frame();
			info.sprite = SP;
			if (spriteJson.Delays.Count > 0)
			{
				info.secondDelay = spriteJson.Delays[variance][frame];
			}

			SpriteData.Variance[variance].Frames.Add(info);
			if (variance >= (spriteJson.Number_Of_Variants - 1))
			{
				variance = 0;
				frame++;
			}
			else
			{
				variance++;
			}
		}
		return SpriteData;

	}	// Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
