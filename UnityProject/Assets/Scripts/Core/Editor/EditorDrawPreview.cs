#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using Logs;
using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Diagnostics;
using System.Reflection;
using Object = UnityEngine.Object;



[CustomEditor(typeof(SpriteDataSO), true)]
[CanEditMultipleObjects]
public class MyObjectEditor : Editor
{
	[InitializeOnLoad]
	public class EditorDrawPreview
	{
		static EditorDrawPreview()
		{
			EditorApplication.projectWindowItemOnGUI -= DrawProjectItem;
			EditorApplication.projectWindowItemOnGUI += DrawProjectItem;
		}
	}


	private static readonly Dictionary<string, SpriteDataSO> guidToSpriteDataSO = new();

	public static HashSet<string> guidIsSprite = new();
	public static HashSet<string> guidIsProcessed = new();

	private static void DrawProjectItem(string guid, Rect selectionRect)
	{

		if (TryGetSpriteData(guid, out var spriteData) == false) return;

		var sprite = spriteData.CurrentSprite;
		if (sprite == null) return;

		var texture = sprite.texture;

		if (texture.isReadable == false)
		{
			Loggy.LogError($"Sprite \"{sprite.name}\" is not read/write enabled. Please enable " +
		                  "Read/Write in the texture's import settings.", Category.Editor);
			return;
		}




		selectionRect.height = selectionRect.width; // Exclude text description
		var spriteRect = sprite.rect;
		var x = spriteRect.x / texture.width;
		var y = spriteRect.y / texture.height;
		var width = spriteRect.width / texture.width;
		var height = spriteRect.height / texture.height;
		var textureRect = new Rect(x, y, width, height);
		var iconRect = GetIconRect(sprite, selectionRect);



		GUI.DrawTexture(selectionRect, BlankTexture, ScaleMode.StretchToFill, false);
		GUI.DrawTextureWithTexCoords(iconRect, texture, textureRect);

	}

	/// <summary>
	/// Tries to get the sprite data entry for a GUID. Returns false if the GUID is not of a SpriteDataSO type. In the
	/// event that the sprite data could not be loaded, this will still return true but with an empty entry where the
	/// sprite data is null.
	/// </summary>
	private static bool TryGetSpriteData(string guid, out SpriteDataEntry entry)
	{
		entry = null;
		if (guidIsSprite.Contains(guid) == false)
		{
			if (RNG.GetRandomNumber(1, 100) > 2) return false;
			if (guidIsProcessed.Contains(guid)) return false;

			var assetPath = AssetDatabase.GUIDToAssetPath(guid);
			var assetType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);

			if (assetType != typeof(SpriteDataSO))
			{
				guidIsProcessed.Add(guid);
				return false;
			}



			var data = AssetDatabase.LoadAssetAtPath<SpriteDataSO>(assetPath);

			if (data == null)
			{
				// An empty entry will be added so we don't get spammed with the same message.
				entry = SpriteDataEntry.Empty;
				Loggy.LogWarning($"Could not load {nameof(SpriteDataSO)} at \"{assetPath}\". " +
				                 "Unable to render the sprite in the asset viewer.", Category.Editor);
			}

			if (data.SpriteDataEntry == null)
			{
				data.SpriteDataEntry = new MyObjectEditor.SpriteDataEntry(data);
			}
			guidIsSprite.Add(guid);
			guidToSpriteDataSO.Add(guid, data);
			entry = (SpriteDataEntry) data.SpriteDataEntry;
			return true;
		}
		if (guidToSpriteDataSO.TryGetValue(guid, out var dataEntry))
		{
			entry = (SpriteDataEntry) dataEntry.SpriteDataEntry;
			return true;
		}

		return false;
	}

	public override Texture2D RenderStaticPreview(string assetPath, Object[] subAssets, int width, int height)
	{
		// Get the GUID of the target asset
		string guid = AssetDatabase.AssetPathToGUID(assetPath);
		guidIsSprite.Add(guid);

		var so = (SpriteDataSO) target;
		guidToSpriteDataSO[guid] = so;

		if (so.SpriteDataEntry == null)
		{
			so.SpriteDataEntry = new MyObjectEditor.SpriteDataEntry(so);
		}

		var spriteData = (MyObjectEditor.SpriteDataEntry) so.SpriteDataEntry;
		var sprite = spriteData.CurrentSprite;

		if (sprite == null) return null;

		var texture = sprite.texture;

		if (texture.isReadable == false)
		{
			Loggy.LogError($"Sprite \"{sprite.name}\" is not read/write enabled. Please enable " +
			               "Read/Write in the texture's import settings.", Category.Editor);
			return null;
		}


		Type t = GetType("UnityEditor.SpriteUtility");
		if (t != null)
		{
			MethodInfo method = t.GetMethod("RenderStaticPreview",
				new Type[] {typeof(Sprite), typeof(Color), typeof(int), typeof(int)});

			if (method != null)

			{
				object ret = method.Invoke("RenderStaticPreview",
					new object[] {sprite, Color.white, width, height});

				if (ret is Texture2D)
					return ret as Texture2D;
			}
		}


		return base.RenderStaticPreview(assetPath, subAssets, width, height);
	}


	private static Type GetType(string TypeName)

	{
		var type = Type.GetType(TypeName);

		if (type != null)

			return type;


		if (TypeName.Contains("."))
		{
			var assemblyName = TypeName.Substring(0, TypeName.IndexOf('.'));

			var assembly = Assembly.Load(assemblyName);

			if (assembly == null)
				return null;

			type = assembly.GetType(TypeName);

			if (type != null)
				return type;
		}


		var currentAssembly = Assembly.GetExecutingAssembly();

		var referencedAssemblies = currentAssembly.GetReferencedAssemblies();

		foreach (var assemblyName in referencedAssemblies)

		{
			var assembly = Assembly.Load(assemblyName);

			if (assembly != null)

			{
				type = assembly.GetType(TypeName);

				if (type != null)

					return type;
			}
		}

		return null;
	}

	public override bool HasPreviewGUI() => true;

	// This method is called to draw a custom GUI for asset previews in the Project view.
	public override void OnPreviewGUI(Rect r, GUIStyle background)
	{
		var so = (SpriteDataSO) target;
		if (so.SpriteDataEntry == null)
		{
			so.SpriteDataEntry = new SpriteDataEntry(so);
		}
		string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(target));
		guidIsSprite.Add(guid);
		guidToSpriteDataSO[guid] = so;

		var spriteData = (SpriteDataEntry) so.SpriteDataEntry;
		var sprite = spriteData.CurrentSprite;

		if (sprite == null) return;

		var texture = sprite.texture;

		if (texture.isReadable == false)
		{
			Loggy.LogError($"Sprite \"{sprite.name}\" is not read/write enabled. Please enable " +
			               "Read/Write in the texture's import settings.", Category.Editor);
			return;
		}

		r.height = r.width; // Exclude text description
		var spriteRect = sprite.rect;
		var x = spriteRect.x / texture.width;
		var y = spriteRect.y / texture.height;
		var width = spriteRect.width / texture.width;
		var height = spriteRect.height / texture.height;
		var textureRect = new Rect(x, y, width, height);
		var iconRect = GetIconRect(sprite, r);

		GUI.DrawTexture(r, BlankTexture, ScaleMode.StretchToFill, false);
		GUI.DrawTextureWithTexCoords(iconRect, texture, textureRect);
	}

	public class SpriteDataEntry
	{
		public static readonly SpriteDataEntry Empty = new(null);

		private SpriteDataSO SpriteData { get; }

		private int VariantIndex { get; set; }

		private int FrameIndex { get; set; }

		private SpriteDataSO.Frame CurrentFrame { get; set; }

		private double NextUpdate { get; set; } = EditorApplication.timeSinceStartup;

		public Sprite CurrentSprite
		{
			get
			{
				if (SpriteData == null) return null;

				UpdateFrame();

				return CurrentFrame?.sprite;
			}
		}

		public SpriteDataEntry(SpriteDataSO spriteData) => SpriteData = spriteData;

		private void UpdateFrame()
		{
			if (SpriteData == null || SpriteData.Variance.Count == 0) return;

			var timeSinceStartup = EditorApplication.timeSinceStartup;

			if (NextUpdate >= timeSinceStartup) return;

			var variants = SpriteData.Variance;

			if (VariantIndex >= variants.Count)
			{
				VariantIndex = 0;
			}

			FrameIndex++;
			if (FrameIndex >= variants[VariantIndex].Frames.Count)
			{
				FrameIndex = 0;
				VariantIndex++;
			}

			if (VariantIndex >= variants.Count)
			{
				VariantIndex = 0;
			}

			var frames = variants[VariantIndex].Frames;

			// In the off chance the selected variant doesn't have frames, skip now and it will move to the next variant later
			if (frames.Count == 0) return;

			CurrentFrame = frames[FrameIndex];
			var delay = CurrentFrame.secondDelay;
			delay = delay <= 0 ? 1f : delay;
			NextUpdate = timeSinceStartup + delay;
		}
	}

	private static Texture2D blankTexture;

	/// <summary>
	/// The basic dark grey background to use for the icons.
	/// </summary>
	private static Texture2D BlankTexture
	{
		get
		{
			if (blankTexture != null) return blankTexture;

			// Can't create assets within InitializeOnLoad static constructors which is why it's created here.
			blankTexture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
			blankTexture.SetPixel(0, 0, new Color32(30, 30, 30, 255));
			blankTexture.Apply();

			return blankTexture;
		}
	}


	private static Rect GetIconRect(Sprite sprite, Rect selectionRect)
	{
		var x = selectionRect.x;
		var y = selectionRect.y;
		var width = selectionRect.width;
		var height = selectionRect.height;
		var spriteRect = sprite.rect;

		// Adjust the icon rect parameters to match the aspect ratio of the sprite
		if (spriteRect.height > spriteRect.width)
		{
			var ratio = spriteRect.width / spriteRect.height;
			x += (1f - ratio) * (width * 0.5f);
			width *= ratio;
		}
		else
		{
			var ratio = spriteRect.height / spriteRect.width;
			y += (1f - ratio) * (height * 0.5f);
			height *= ratio;
		}

		return new Rect(x, y, width, height);
	}
}
#endif