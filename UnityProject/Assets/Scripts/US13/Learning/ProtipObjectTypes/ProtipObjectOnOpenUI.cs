using UnityEngine;
using US13.Objects;

namespace US13.Learning.ProtipObjectTypes
{
	public class ProtipObjectOnOpenUI : ProtipObject
	{
		[SerializeField] private HasNetworkTab networkTab;

		private void OnEnable()
		{
			networkTab.OnShowUI += Trigger;
		}

		private void OnDisable()
		{
			networkTab.OnShowUI -= Trigger;
		}

		private void Trigger(GameObject picker)
		{
			TriggerTip(picker);
		}
	}
}