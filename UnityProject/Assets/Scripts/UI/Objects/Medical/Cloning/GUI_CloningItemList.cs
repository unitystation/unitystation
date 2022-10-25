using Objects.Medical;
using UI.Core.NetUI;

namespace UI.Objects.Medical.Cloning
{
	public class GUI_CloningItemList : NetUIDynamicList
	{
		public void AddItems(int count)
		{
			for (int i = 0; i < count; i++)
			{
				AddItem();
			}
		}

		public bool AddItem()
		{
			var entryArray = Entries;
			for (var i = 0; i < entryArray.Count; i++)
			{
				DynamicEntry entry = entryArray[i];
				var item = entry as GUI_CloningRecordItem;
			}

			//add new entry
			GUI_CloningRecordItem newEntry = Add() as GUI_CloningRecordItem;
			if (!newEntry)
			{
				return false;
			}

			//rescan elements  and notify
			NetworkTabManager.Instance.Rescan(containedInTab.NetTabDescriptor);
			UpdatePeepers();

			return true;
		}

		public bool RemoveItem(CloningRecord record)
		{
			foreach (var pair in EntryIndex)
			{
				if (((GUI_CloningRecordItem)pair.Value)?.cloningRecord == record)
				{
					Remove(pair.Key);
					return true;
				}
			}
			UpdatePeepers();
			return false;
		}
	}
}
