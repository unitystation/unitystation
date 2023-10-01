#if UNITY_EDITOR
using System.Collections.Generic;
using Logs;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class EditorDrawPreview
{
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

	private static readonly Dictionary<string, SpriteDataEntry> guidToSpriteDataEntry = new();

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

	static EditorDrawPreview()
	{
		EditorApplication.projectWindowItemOnGUI += DrawProjectItem;
	}

	private static void DrawProjectItem(string guid, Rect selectionRect)
	{
		if (EditorPrefs.GetBool("editorPreviewsDisable", false)) return;
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
		if (guidToSpriteDataEntry.TryGetValue(guid, out var dataEntry))
		{
			entry = dataEntry;
			return true;
		}

		var assetPath = AssetDatabase.GUIDToAssetPath(guid);
		var assetType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);

		if (assetType != typeof(SpriteDataSO))
		{
			entry = null;
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
		else
		{
			entry = new SpriteDataEntry(data);
		}

		guidToSpriteDataEntry.Add(guid, entry);
		return true;
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

	[MenuItem("Tools/DisableEnableEditorSpritePreviews #&]")]
	public static void CompiledDammit()
	{
		EditorPrefs.SetBool("editorPreviewsDisable", !EditorPrefs.GetBool("editorPreviewsDisable", false));
	}
}
#endif
