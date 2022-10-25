using System;
using System.Collections.Generic;
using UI.Core.NetUI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI.Objects.Medical
{
	public class DNAStrandElement : DynamicEntry
	{
		public enum Location
		{
			List = 0,
			BodyPartTarget = 1,
			Mutation = 2,
			CustomisationMutation = 3,
			SpeciesMutation = 4,
		}

		public DNAMutationData.DNAPayload Payload;
		public string target;
		public Location SetLocation;

		public NetParentSetter NetParentSetter;
		private WindowDragUIIinteract WindowDragUIIinteract;
		public GUI_DNAConsole GUI_DNAConsole;
		public NetButton NetButton;

		public NetText_label label;

		//Whatever functions you want

		public void SetValues(DNAMutationData.DNAPayload InPayload, string Intarget, DNAStrandElement.Location InSetLocation)
		{
			Payload = InPayload;
			target = Intarget;
			SetLocation = InSetLocation;
			NetParentSetter.SetParentViaID((int) InSetLocation);

			//label

			if (Payload.TargetMutationSO != null)
			{
				label.SetValue("Mutation " + Payload.TargetMutationSO.name);
			}

			if (Payload.RemoveTargetMutationSO != null)
			{
				label.SetValue("Remove Mutation " + Payload.RemoveTargetMutationSO.name);
			}

			if (Payload.MutateToBodyPart != null)
			{
				label.SetValue("To Bodypart " + Payload.MutateToBodyPart.name);
			}

			if (string.IsNullOrEmpty(target) == false)
			{
				label.SetValue("Body part Name target " + target);
			}

			if (string.IsNullOrEmpty(Payload.CustomisationReplaceWith) == false || string.IsNullOrEmpty(Payload.CustomisationTarget) == false)
			{
				label.SetValue($"target {Payload.CustomisationTarget} Replace with {Payload.CustomisationReplaceWith}");
			}

		}

		public void Awake()
		{
			GUI_DNAConsole = this.GetComponentInParent<GUI_DNAConsole>();
			WindowDragUIIinteract = this.GetComponent<WindowDragUIIinteract>();
			WindowDragUIIinteract.OnDropTarget.AddListener(OnDrop);
			NetButton = GetComponentInChildren<NetButton>();
		}


		public void OnDrop(List<RaycastResult> Targets)
		{
			if (Targets.Count <= 1)
			{

				if (Payload.TargetMutationSO == null)
				{
					//Destroy
					NetButton.ExecuteClient();
				}
				else
				{
					NetParentSetter.SetParentViaID((int)Location.Mutation);
				}





				return;
			}

			foreach (var Target in Targets)
			{
				if (Target.gameObject.TryGetComponent<NetParentSetterTarget>(out var Found))
				{
					if (NetParentSetter.IsValidParentViaNetParentSetterTarget(Found))
					{
						//maybe to do to add a switch statement to not allow it if it's a certain type?
						NetParentSetter.SetParentViaNetParentSetterTarget(Found);
						return;
					}
				}
			}
			//If nothing was found
			transform.localPosition = Vector3.zero;

			//
			//remove?
		}



		public void MasterRemoveSelfFromList()
		{
			//Destroy
			GUI_DNAConsole.DNAStrandList.MasterRemoveItem(this);
		}
	}
}
