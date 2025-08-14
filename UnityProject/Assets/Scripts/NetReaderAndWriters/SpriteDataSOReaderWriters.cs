using System;
using Logs;
using Mirror;

public static class SpriteDataSOReaderWriters
{
	public static SpriteDataSO Deserialize(this NetworkReader reader)
	{
		try
		{
			var index = reader.ReadUInt();
			return SpriteCatalogue.ResistantCatalogue[(int)index];
		}
		catch (Exception e)
		{
			Loggy.Error($"An error occured while deserializing sprite data.\n {e}");
			return SpriteCatalogue.ResistantCatalogue[0];
		}
	}

	public static void Serialize(this NetworkWriter writer, SpriteDataSO spriteDataSo)
	{
		if (spriteDataSo == null)
		{
			Loggy.Warning("Null sprite detected. Giving default value of 0.");
			writer.WriteUInt(0);
			return;
		}
		try
		{
			writer.WriteUInt((uint)spriteDataSo.SetID);
		}
		catch (Exception e)
		{
			Loggy.Error($"An error occured while serializing sprite data.\n {e}");
			writer.WriteUInt(0);
		}
	}
}