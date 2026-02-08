using Logs;
using SecureStuff;
using UnityEngine;
using US13.HealthV2.Living;
using US13.HealthV2.Living.CirculatorySystem;

namespace US13.Actions.V2.Trackers
{
	public class BodyPartActionTracker : MonoBehaviour, IActionButtonTracker
	{
		public BodyPart relatedBodyPart;

		[field: SerializeField] public SerializableDictionary<ActionButtonData, SerializedAction> ActionData { get; set; }
		public ActionManager TargetActionManager { get; set; }

		private void Awake()
		{
			relatedBodyPart ??= GetComponent<BodyPart>();
			if (relatedBodyPart == null)
			{
				Loggy.Error("Cannot initialize BodyPartActionTracker without a related BodyPart.");
				return;
			}

			relatedBodyPart.OnAddedToBody += AddedToBody;
			relatedBodyPart.OnRemovedFromBody += RemovedFromBody;
		}

		private void RemovedFromBody(LivingHealthMasterBase obj)
		{
			if (TargetActionManager == null)
			{
				Loggy.Error("TargetActionManager is null. Cannot remove action buttons.");
				return;
			}
			WhenHolderIsOutOfRange();
		}

		private void AddedToBody(LivingHealthMasterBase related)
		{
			if (related == null)
			{
				Loggy.Error("Related LivingHealthMasterBase is null. Cannot initialize ActionManager.");
				return;
			}
			TargetActionManager = related.playerScript.PlayerButtonedActions;
			WhenHolderIsInRange();
		}

		public void WhenHolderIsInRange()
		{
			foreach (var data in ActionData.Keys)
			{
				TargetActionManager.RegisterNewAction(data, ActionData[data].Invoke);
			}
		}

		public void WhenHolderIsOutOfRange()
		{
			foreach (var actionData in ActionData.Keys)
			{
				TargetActionManager.UnregisterAction(actionData);
			}
			TargetActionManager = null;
		}
	}
}