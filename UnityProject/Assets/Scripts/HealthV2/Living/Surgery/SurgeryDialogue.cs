using System.Collections.Generic;
using UnityEngine;
using HealthV2;
using Player;

namespace UI
{
	public class SurgeryDialogue : MonoBehaviour
	{
		public GameObject ScrollList;
		public SurgicalProcessItem ListItem;

		public SpriteDataSO PickSprite;

		public static SurgeryDialogue Instant;

		public List<SurgicalProcessItem> OpenItems = new List<SurgicalProcessItem>();

		private Dissectible dissectible;
		private List<BodyPart> bodyParts;
		private bool TopLayer = false;

		public void OnEnable()
		{
			Instant = this;
		}

		public void CloseDialogue()
		{
			this.SetActive(false);
		}

		public void ShowDialogue(Dissectible dissectible, List<BodyPart> BodyParts, bool TopLayer = false)
		{
			transform.localPosition = Vector3.zero;
			this.SetActive(true);
			this.dissectible = dissectible;
			this.bodyParts = BodyParts;
			this.TopLayer = TopLayer;
			Refresh();
		}

		private void ShowDialogue(Dissectible Dissectible, BodyPart BodyPart)
		{
			this.transform.localPosition = Vector3.zero;
			Clear();
			this.SetActive(true);
			foreach (var procedure in BodyPart.SurgeryProcedureBase)
			{
				if (procedure is CloseProcedure || procedure is ImplantProcedure) continue;
				var newItem = Instantiate(ListItem, ScrollList.transform);
				newItem.ProcedureToChoose(BodyPart.gameObject, () => { StartProcedure(Dissectible, BodyPart, procedure); },
					procedure.ProcedureSprite, procedure.ProcedureName);
				OpenItems.Add(newItem);
			}
		}

		private void StartProcedure(Dissectible Dissectible, BodyPart bodyPart, SurgeryProcedureBase SurgeryProcedureBase)
		{
			Clear();
			RequestSurgery.Send(bodyPart.OrNull()?.gameObject, Dissectible.gameObject, SurgeryProcedureBase);
			this.SetActive(false);
		}

		private void Clear()
		{
			foreach (var item in OpenItems)
			{
				Destroy(item.gameObject);
			}

			OpenItems.Clear();
		}

		private void Refresh()
		{
			Clear();
			if (dissectible.ProcedureInProgress) TopLayer = true;
			foreach (var bodyPart in bodyParts)
			{
				var newItem = Instantiate(ListItem, ScrollList.transform);
				newItem.BodyToChoose(bodyPart, () => { ShowDialogue(dissectible, bodyPart); }, PickSprite, "Pick");
				OpenItems.Add(newItem);
			}

			if (TopLayer == false)
			{
				foreach (var procedure in dissectible.BodyPartIsOn.SurgeryProcedureBase)
				{
					if (procedure is not (CloseProcedure or ImplantProcedure)) continue;
					var newItem = Instantiate(ListItem, ScrollList.transform);
					newItem.ProcedureToChoose(dissectible.currentlyOn, () => { StartProcedure(dissectible, dissectible.BodyPartIsOn, procedure); },
						procedure.ProcedureSprite, procedure.ProcedureName);
					OpenItems.Add(newItem);
				}
			}
			else
			{
				var procedure = dissectible.GetComponent<PlayerSprites>().RaceBodyparts.Base.RootImplantProcedure;
				var newItem = Instantiate(ListItem, ScrollList.transform);
				newItem.ProcedureToChoose(dissectible.gameObject, () => { StartProcedure(dissectible, dissectible.BodyPartIsOn, procedure); },
					procedure.ProcedureSprite, procedure.ProcedureName);
				OpenItems.Add(newItem);
			}
		}
	}
}