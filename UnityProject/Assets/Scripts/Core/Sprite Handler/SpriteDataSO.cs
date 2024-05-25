using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using NaughtyAttributes;
using ScriptableObjects;

[CreateAssetMenu(fileName = "SpriteData", menuName = "ScriptableObjects/SpriteData")]
public class SpriteDataSO : SOTracker
{
	public override SpriteDataSO Sprite => this;

	public List<Variant> Variance = new List<Variant>();
	public bool IsPalette = false;

	[NonSerialized] public int SetID = -1;

	public string DisplayName;

	[Serializable]
	public struct Variant
	{
		public List<Frame> Frames;
	}


	[Serializable]
	public class Frame
	{
		public Sprite sprite;
		public float secondDelay;
	}
}

public static class SpriteDataSOSerialization
{
    public static void WriteSpriteDataSO(this NetworkWriter writer, SpriteDataSO spriteDataSO)
    {
        writer.WriteBool(spriteDataSO.IsPalette);
        writer.WriteString(spriteDataSO.DisplayName);
        writer.WriteInt(spriteDataSO.SetID);

        writer.WriteInt(spriteDataSO.Variance.Count);
        foreach (var variant in spriteDataSO.Variance)
        {
            writer.WriteVariant(variant);
        }
    }

    public static SpriteDataSO ReadSpriteDataSO(this NetworkReader reader)
    {
        SpriteDataSO spriteDataSO = ScriptableObject.CreateInstance<SpriteDataSO>();
        spriteDataSO.IsPalette = reader.ReadBool();
        spriteDataSO.DisplayName = reader.ReadString();
        spriteDataSO.SetID = reader.ReadInt();

        int variantCount = reader.ReadInt();
        spriteDataSO.Variance = new List<SpriteDataSO.Variant>(variantCount);
        for (int i = 0; i < variantCount; i++)
        {
            spriteDataSO.Variance.Add(reader.ReadVariant());
        }

        return spriteDataSO;
    }

    public static void WriteVariant(this NetworkWriter writer, SpriteDataSO.Variant variant)
    {
        writer.WriteInt(variant.Frames.Count);
        foreach (var frame in variant.Frames)
        {
            writer.WriteFrame(frame);
        }
    }

    public static SpriteDataSO.Variant ReadVariant(this NetworkReader reader)
    {
        SpriteDataSO.Variant variant = new SpriteDataSO.Variant();
        int frameCount = reader.ReadInt();
        variant.Frames = new List<SpriteDataSO.Frame>(frameCount);
        for (int i = 0; i < frameCount; i++)
        {
            variant.Frames.Add(reader.ReadFrame());
        }
        return variant;
    }

    public static void WriteFrame(this NetworkWriter writer, SpriteDataSO.Frame frame)
    {
        writer.WriteSprite(frame.sprite);
        writer.WriteFloat(frame.secondDelay);
    }

    public static SpriteDataSO.Frame ReadFrame(this NetworkReader reader)
    {
        SpriteDataSO.Frame frame = new SpriteDataSO.Frame();
        frame.sprite = reader.ReadSprite();
        frame.secondDelay = reader.ReadFloat();
        return frame;
    }

    // Helper methods for serializing and deserializing Sprite
    public static void WriteSprite(this NetworkWriter writer, Sprite sprite)
    {
        if (sprite == null)
        {
            writer.WriteBool(false);
        }
        else
        {
            writer.WriteBool(true);
            writer.WriteString(sprite.name);
            writer.WriteTexture2D(sprite.texture);
        }
    }

    public static Sprite ReadSprite(this NetworkReader reader)
    {
        if (!reader.ReadBool())
        {
            return null;
        }
        string spriteName = reader.ReadString();
        Texture2D texture = reader.ReadTexture2D();
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
    }

    // Helper methods for serializing and deserializing Texture2D
    public static void WriteTexture2D(this NetworkWriter writer, Texture2D texture)
    {
        byte[] textureData = texture.EncodeToPNG();
        writer.WriteBytesAndSize(textureData);
    }

    public static Texture2D ReadTexture2D(this NetworkReader reader)
    {
        byte[] textureData = reader.ReadBytesAndSize();
        Texture2D texture = new Texture2D(2, 2);
        texture.LoadImage(textureData);
        return texture;
    }
}
