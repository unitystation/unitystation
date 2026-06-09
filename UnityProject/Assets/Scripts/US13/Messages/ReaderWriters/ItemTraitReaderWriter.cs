using Mirror;
using SecureStuff;
using US13.Items.Traits;

namespace US13.Messages.ReaderWriters
{
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
}
