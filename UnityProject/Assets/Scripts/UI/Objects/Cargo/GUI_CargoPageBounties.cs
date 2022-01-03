using System.Collections.Generic;
using UnityEngine;
using UI.Core.NetUI;
using Systems.Cargo;

namespace UI.Objects.Cargo
{
	public class GUI_CargoPageBounties : GUI_CargoPage
	{
		[SerializeField]
		private EmptyItemList orderList;

		public override void OpenTab()
		{
			CargoManager.Instance.OnBountiesUpdate.AddListener(UpdateTab);
		}

		public override void UpdateTab()
		{
			var bounties = CargoManager.Instance.ActiveBounties;
			orderList.Clear();
			orderList.AddItems(bounties.Count);
			for (var i = 0; i < bounties.Count; i++)
			{
				var item = (GUI_CargoBounty)orderList.Entries[i];
				item.SetValues(bounties[i]);
			}
		}
	}
}
