using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using SecureStuff;
using UnityEngine;

public static class  ItemTraitReaderWriter
{
	public static ItemTrait Deserialize(this NetworkReader reader)
	{
		return (ItemTrait) Librarian.Page.DeSerialiseValue(reader.ReadString(), typeof(ItemTrait));
	}

	public static void Serialize(this NetworkWriter writer, ItemTrait message)
	{
		writer.WriteString(message.ForeverID);
	}
}
