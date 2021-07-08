using System;
using System.Collections;
using System.Collections.Generic;
using HealthV2;
using UnityEngine;

public class SurgeryDialogue : MonoBehaviour
{
	public GameObject ScrollList;
	public SurgicalProcessItem ListItem;

	public SpriteDataSO PickSprite;

	public static SurgeryDialogue Instant;


	public List<SurgicalProcessItem> OpenItems = new List<SurgicalProcessItem>();

	public void OnEnable()
	{
		Instant = this;
	}

	public void Awake()
	{
		//this.SetActive(false);
	}


	public void CloseDialogue()
	{
		this.SetActive(false);
	}

	public void ShowDialogue(Dissectible Dissectible, List<BodyPart> BodyParts, bool TopLayer = false)
	{
		this.transform.localPosition = Vector3.zero;
		this.SetActive(true);
		Clear();
		foreach (var bodyPart in BodyParts)
		{
			var newItem = Instantiate(ListItem, ScrollList.transform);
			newItem.BodyToChoose(bodyPart, () => { ShowDialogue(Dissectible, bodyPart); }, PickSprite, "Pick");
			OpenItems.Add(newItem);
		}

		if (TopLayer == false)
		{
			foreach (var Procedure in Dissectible.BodyPartIsOn.SurgeryProcedureBase)
			{
				if (Procedure is CloseProcedure || Procedure is ImplantProcedure)
				{
					var newItem = Instantiate(ListItem, ScrollList.transform);
					newItem.ProcedureToChoose( Dissectible.currentlyOn, () => { StartProcedure(Dissectible,  Dissectible.BodyPartIsOn, Procedure); },
						Procedure.ProcedureSprite, Procedure.ProcedureName);
					OpenItems.Add(newItem);
				}
			}
		}
		else
		{
			var Procedure = Dissectible.GetComponent<PlayerSprites>().RaceBodyparts.Base.RootImplantProcedure;
			var newItem = Instantiate(ListItem, ScrollList.transform);
			newItem.ProcedureToChoose( Dissectible.gameObject, () => { StartProcedure(Dissectible,  Dissectible.BodyPartIsOn, Procedure); },
				Procedure.ProcedureSprite, Procedure.ProcedureName);
			OpenItems.Add(newItem);

		}
	}


	public void ShowDialogue(Dissectible Dissectible, BodyPart BodyPart)
	{
		this.transform.localPosition = Vector3.zero;
		Clear();
		this.SetActive(true);
		foreach (var Procedure in BodyPart.SurgeryProcedureBase)
		{
			if (Procedure is CloseProcedure || Procedure is ImplantProcedure) continue;
			var newItem = Instantiate(ListItem, ScrollList.transform);
			newItem.ProcedureToChoose(BodyPart.gameObject, () => { StartProcedure(Dissectible, BodyPart, Procedure); },
				Procedure.ProcedureSprite, Procedure.ProcedureName);
			OpenItems.Add(newItem);
		}
	}

	public void StartProcedure(Dissectible Dissectible, BodyPart bodyPart, SurgeryProcedureBase SurgeryProcedureBase)
	{
		Clear();
		this.SetActive(false);
		RequestSurgery.Send(bodyPart?.gameObject, Dissectible.gameObject, SurgeryProcedureBase);
		// send message to server
		// Dissectible.currentlyOn = bodyPart;
		// Dissectible.ThisPresentProcedure.SetupProcedure(Dissectible, bodyPart, SurgeryProcedureBase);
	}


	public void Clear()
	{
		foreach (var item in OpenItems)
		{
			Destroy(item.gameObject);
		}

		OpenItems.Clear();
	}
}