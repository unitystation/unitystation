using System.Linq;
using US13.Managers.NetworkManagement;
using US13.UI.Core.Net.Elements.Dynamic;

namespace US13.UI.Core
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
			AddBulk(new string[count]);
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
	}
}
