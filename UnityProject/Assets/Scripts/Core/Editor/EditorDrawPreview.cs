using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

[InitializeOnLoad]
public class MyProjectViewExtentions
{
	public static int FrameLoops = 3;

	static MyProjectViewExtentions()
	{
		EditorApplication.projectWindowItemOnGUI += DrawProjectItem;
	}

	public static Dictionary<string, DatabaseEntry> Dictionaryguid = new Dictionary<string, DatabaseEntry>();

	public class DatabaseEntry
	{
		public DatabaseEntry(SpriteDataSO _spriteDataSO,
			Texture2D _generatedTexture2D,
			SpriteDataSO.Frame _PresentFrame)
		{
			spriteDataSO = _spriteDataSO;
			Textdict = new Dictionary<SpriteDataSO.Frame, Texture2D> {[_PresentFrame] = _generatedTexture2D};
			TimeSet = DateTime.Now;
			PresentFrame = _PresentFrame;
		}

		public System.DateTime TimeSet;
		public int Variant = 0;
		public int Frame = 0;
		public int FrameLoop = 0;
		public SpriteDataSO spriteDataSO;
		public SpriteDataSO.Frame PresentFrame;
		public Dictionary<SpriteDataSO.Frame, Texture2D> Textdict;
	}

	private static void DrawProjectItem(string guid, Rect selectionRect)
	{
		var sTing = AssetDatabase.GUIDToAssetPath(guid);
		if (sTing.Contains(".asset"))
		{
			Texture2D mainTex;
			if (Dictionaryguid.ContainsKey(guid))
			{
				mainTex = GetCorrectTexture(Dictionaryguid[guid]);
			}
			else
			{
				var spriteDataSO = AssetDatabase.LoadAssetAtPath<SpriteDataSO>(sTing);

				if (spriteDataSO == null) return;
				if (spriteDataSO.Variance.Count <= 0 || spriteDataSO.Variance[0].Frames.Count <= 0 ||
				    spriteDataSO.Variance[0].Frames[0].sprite == null) return;
				TextureImporter importer =
					AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(spriteDataSO.Variance[0].Frames[0].sprite)) as
						TextureImporter;
				if (importer.isReadable == false)
				{
					Logger.Log("hey, Texture read and write is not enabled for this Sprite " +
					           spriteDataSO.Variance[0].Frames[0].sprite +
					           "Please update the values on the import settings to make it Read and write");
					return;
				}

				mainTex = CopySprite(GenerateNewTexture2D(), spriteDataSO.Variance[0].Frames[0].sprite);
				var DBin = new DatabaseEntry(spriteDataSO, mainTex, spriteDataSO.Variance[0].Frames[0]);
				Dictionaryguid[guid] = DBin;
			}

			selectionRect.height = selectionRect.width;
			Texture2D icon = mainTex;
			if (icon != null)
			{
				GUI.DrawTexture(selectionRect, icon);
			}
		}
	}

	public static Texture2D GetCorrectTexture(DatabaseEntry Db)
	{
		var SO = Db.spriteDataSO;
		var timeElapsed = ((DateTime.Now - Db.TimeSet).Milliseconds / 1000f);

		if (timeElapsed >= Db.PresentFrame.secondDelay)
		{
			Db.Frame++;
			Db.TimeSet = SO.Variance[Db.Variant].Frames.Count == 1 ? DateTime.Now.AddSeconds(1) : DateTime.Now;

			if (Db.Frame >= SO.Variance[Db.Variant].Frames.Count)
			{
				Db.Frame = 0;
				Db.FrameLoop++;
			}

			if (Db.FrameLoop > FrameLoops)
			{
				Db.FrameLoop = 0;
				Db.Variant++;
				if (SO.Variance.Count > Db.Variant == false)
				{
					Db.Variant = 0;
				}
			}

			if (Db.Frame >= SO.Variance[Db.Variant].Frames.Count)
			{
				Db.Frame = 0;
			}

			Db.PresentFrame = SO.Variance[Db.Variant].Frames[Db.Frame];
			if (Db.Textdict.ContainsKey(Db.PresentFrame) == false)
			{
				Db.Textdict[Db.PresentFrame] = CopySprite(GenerateNewTexture2D(), Db.PresentFrame.sprite);
			}
		}

		return Db.Textdict[Db.PresentFrame];
	}

	public static Texture2D GetSpriteRenderer(GameObject GameO)
	{
		var SRs = GameO.GetComponentsInChildren<SpriteRenderer>();
		if (SRs.Length == 0) return null;
		var T2D = GenerateNewTexture2D();
		foreach (var SR in SRs)
		{
			if (SR.enabled && SR.sprite != null)
			{
				TextureImporter importer =
					AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(SR.sprite)) as TextureImporter;
				if (importer.isReadable == false)
				{
					Logger.Log("hey, Texture read and write is not enabled for this Sprite " + SR.sprite +
					           "Please update the values on the import settings to make it Read and write");
					return T2D;
				}

				T2D = CopySprite(T2D, SR.sprite);
			}
		}

		return T2D;
	}

	public static Texture2D GenerateNewTexture2D()
	{
		var mainTex = new Texture2D(32, 32, TextureFormat.ARGB32, false);
		mainTex.filterMode = FilterMode.Point;
		mainTex.alphaIsTransparency = true;
		Unity.Collections.NativeArray<Color32> data = mainTex.GetRawTextureData<Color32>();
		for (int xy = 0; xy < data.Length; xy++)
		{
			data[xy] = new Color32(30, 30, 30, 255);
			//data[xy] = new Color(0.15f, 0.15f, 0.15f, 1f);
		}

		mainTex.Apply();
		return mainTex;
	}

	public static Texture2D CopySprite(Texture2D mainTex, Sprite NewSprite)
	{
		int xx = 0;
		int yy = 0;


		for (int x = (int) NewSprite.textureRect.position.x;
			x < (int) NewSprite.textureRect.position.x + NewSprite.rect.width;
			x++)
		{
			for (int y = (int) NewSprite.textureRect.position.y;
				y < NewSprite.textureRect.position.y + NewSprite.rect.height;
				y++)
			{
				var Pix = NewSprite.texture.GetPixel(x, y);
				if (Pix.a > 0f)
				{
					//Logger.Log(yy + " <XX YY> " + xx + "   " +  x + " <X Y> " + y  );
					mainTex.SetPixel(xx, yy, Pix);
				}
				else
				{
					mainTex.SetPixel(xx, yy, new Color32(30, 30, 30, 255));
				}

				yy = yy + 1;
			}

			yy = 0;
			xx = xx + 1;
		}

		mainTex.Apply();

		return mainTex;
	}
}
#endif