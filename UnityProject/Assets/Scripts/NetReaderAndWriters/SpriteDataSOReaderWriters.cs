using Mirror;
using UnityEngine;

public static class SpriteDataSOReaderWriters
{
	public static SpriteDataSO Deserialize(this NetworkReader reader)
	{
		var Index = reader.ReadUInt();
		return SpriteCatalogue.ResistantCatalogue[(int)Index];
	}

	public static void Serialize(this NetworkWriter writer, SpriteDataSO spriteDataSo)
	{
		writer.WriteUInt((uint)spriteDataSo.SetID);
	}
}