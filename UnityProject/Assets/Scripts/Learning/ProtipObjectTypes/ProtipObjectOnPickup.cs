using UnityEngine;

namespace Learning.ProtipObjectTypes
{
	public class ProtipObjectOnPickup : ProtipObject
	{

		public void Start()
		{
			PlayerManager.LocalPlayerScript.DynamicItemStorage.OnContentsChangeClient.AddListener(OnItemsChanged);
		}

		protected override bool TriggerConditions()
		{
			return true;
		}

		private void OnItemsChanged()
		{
			TriggerTip();
		}
	}
}