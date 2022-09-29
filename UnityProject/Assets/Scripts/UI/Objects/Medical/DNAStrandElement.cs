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
			SetBodyPartTarget = 5,
		}

		public DNAMutationData.DNAPayload Payload;
		public string target;
		public Location SetLocation;

		public NetParentSetter NetParentSetter;
		private WindowDragUIIinteract WindowDragUIIinteract;
		public GUI_DNAConsole GUI_DNAConsole;
		public NetButton NetButton;

		//Whatever functions you want

		public void SetValues(DNAMutationData.DNAPayload InPayload, string Intarget, DNAStrandElement.Location InSetLocation)
		{
			Payload = InPayload;
			target = Intarget;
			SetLocation = InSetLocation;
			NetParentSetter.SetParentViaID((int) InSetLocation);
			//TODO NET STUFF!!!!!!!!

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
			Logger.LogError(Targets.Count.ToString());

			if (Targets.Count <= 1)
			{
				//Destroy
				NetButton.ExecuteClient();


				return;
			}

			foreach (var Target in Targets)
			{
				if (Target.gameObject.TryGetComponent<NetParentSetterTarget>(out var Found))
				{
					if (NetParentSetter.IsValidParentViaNetParentSetterTarget(Found))
					{
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
			GUI_DNAConsole.DNAStrandList.MasterRemoveItem(this);
		}
	}
}
