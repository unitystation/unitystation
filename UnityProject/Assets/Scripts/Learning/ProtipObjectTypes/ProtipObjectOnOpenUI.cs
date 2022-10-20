using Objects;
using UnityEngine;

namespace Learning.ProtipObjectTypes
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