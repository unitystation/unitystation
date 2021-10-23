using System.Collections;
using System.Collections.Generic;
using Initialisation;
using Messages.Client;
using UnityEngine;

namespace Hacking
{
	public class GUI_HackingWire : MonoBehaviour
	{
		[SerializeField] private GameObject wireStart = null;
		public GUI_HackingPort startNode;

		[SerializeField] private GameObject wireEnd = null;

		public GUI_HackingPort endNode;

		[SerializeField] private GameObject wireBody = null;

		private GUI_CablePanel GUI_CablePanel;

		public GUI_CablePanel.CableData ThisCableData;

		public void SetUp(GUI_CablePanel.CableData CableData, GUI_CablePanel InGUI_CablePanel)
		{
			GUI_CablePanel = InGUI_CablePanel;
			ThisCableData = CableData;

			LoadManager.RegisterActionDelayed(SetUpPositions, 2);
		}

		public void SetUpPositions()
		{
			if (ThisCableData.IDConnectedFrom != -1)
			{
				SetStartUINode(GUI_CablePanel.GUI_Hacking.Outputs.IDtoPort[ThisCableData.IDConnectedFrom]);
			}

			if (ThisCableData.IDConnectedTo != -1)
			{
				SetEndUINode(GUI_CablePanel.GUI_Hacking.Inputs.IDtoPort[ThisCableData.IDConnectedTo]);
			}
		}


		public void SetStartUINode(GUI_HackingPort startNode)
		{
			this.startNode = startNode;
			RectTransform nodeRectTransform = startNode.GetComponent<RectTransform>();
			RectTransform wireStartRectTransform = wireStart.GetComponent<RectTransform>();

			wireStartRectTransform.position = nodeRectTransform.position;
			PositionWireBody();
		}

		public void SetEndUINode(GUI_HackingPort endNode)
		{
			if (endNode == null) return;

			this.endNode = endNode;
			RectTransform nodeRectTransform = endNode.GetComponent<RectTransform>();
			RectTransform wireEndRectTransform = wireEnd.GetComponent<RectTransform>();

			wireEndRectTransform.position = nodeRectTransform.position;
			PositionWireBody();
		}

		[NaughtyAttributes.Button()]
		public void PositionWireBody()
		{
			RectTransform wireStartRectTransform = wireStart.GetComponent<RectTransform>();
			RectTransform wireEndRectTransform = wireEnd.GetComponent<RectTransform>();
			RectTransform wireBodyRectTransform = wireBody.GetComponent<RectTransform>();

			Vector2 dif = (wireEndRectTransform.localPosition - wireStartRectTransform.localPosition);

			Vector2 norm = dif.normalized;
			float dist = dif.magnitude;
			float angle = -Vector2.SignedAngle(norm, Vector2.up);

			Vector2 wireOrigin = dist * 0.5f * norm + (Vector2) wireStartRectTransform.localPosition;

			wireBodyRectTransform.localPosition = wireOrigin;

			Vector2 oldSize = wireBodyRectTransform.sizeDelta;

			//* (2 - UIManager.Instance.transform.localScale.x)
			wireBodyRectTransform.sizeDelta =
				new Vector2(oldSize.x,
					dist); //Need to add this scaling here, because for some reason, the entire UI is scaled by 0.67? Iunno why.

			Vector3 rotation = wireBodyRectTransform.transform.eulerAngles;
			rotation.z = angle;
			wireBodyRectTransform.transform.eulerAngles = rotation;
		}

		public void Remove()
		{
			Pickupable handItem = PlayerManager.LocalPlayerScript.Equipment.ItemStorage.GetActiveHandSlot().Item;
			if (handItem != null)
			{
				if (Validations.HasItemTrait(handItem.gameObject, CommonTraits.Instance.Wirecutter))
				{
					RequestHackingInteraction.Send(GUI_CablePanel.GUI_Hacking.HackProcess.gameObject,
						ThisCableData.CableNetuID,
						0, 0,
						RequestHackingInteraction.InteractionWith.CutWire);
				}
			}
		}
	}
}