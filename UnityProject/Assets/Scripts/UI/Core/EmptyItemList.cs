using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UI.Core.NetUI
{
	/// <summary>
	/// This class exists so you can create empty item entries
	/// without need for gameobject(like in SpawnedObjectsList).
	/// This will be obsolete once generic lists arrive.
	/// </summary>
	public class EmptyItemList : NetUIDynamicList
	{
		public void AddItems(int count)
		{
			for (int i = 0; i < count; i++)
			{
				Add();
			}
			NetworkTabManager.Instance.Rescan(containedInTab.NetTabDescriptor);
			UpdatePeepers();
		}

		public void SetItems(int count)
		{
			while (count > Entries.Count)
			{
				Add();
			}

			while (count < Entries.Count)
			{
				Remove(Entries.Last().name);
			}
			NetworkTabManager.Instance.Rescan(containedInTab.NetTabDescriptor);
			UpdatePeepers();
		}

		public DynamicEntry AddItem()
		{
			var newEntry = Add();

			// rescan elements and notify
			NetworkTabManager.Instance.Rescan(containedInTab.NetTabDescriptor);
			UpdatePeepers();

			return newEntry;
		}


		public void MasterRemoveItem(DynamicEntry EntryToRemove)
		{
			Remove(EntryToRemove.name);

			// rescan elements and notify
			NetworkTabManager.Instance.Rescan(containedInTab.NetTabDescriptor);
			UpdatePeepers();
		}
	}
}
