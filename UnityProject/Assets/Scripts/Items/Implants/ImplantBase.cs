using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ItemAttributesV2))]
public class ImplantBase : MonoBehaviour
{
	[SerializeField]
	private List<ImplantProperty> properties = new List<ImplantProperty>();

	private ItemAttributesV2 attributes;

	//This should be utilized in most implants so as to make changing the effectivenss of it easy.
	//Some organs wont boil down to just one efficiency score, so you'll have to keep that in mind.
	[SerializeField]
	[Tooltip("This is a generic variable representing the 'efficieny' of the implant." +
	         "Can be modified by implant modifiers.")]
	private float efficiency = 1;

	[SerializeField]
	[Tooltip("Do we consume any reagent in our blood?")]
	private bool isBloodReagentConsumed = false;

	[SerializeField]
	[Tooltip("How much blood reagent do we actually consume per second?")]
	private float bloodReagentConsumed = 0.15f;

	[SerializeField]
	[Tooltip("How much blood reagent is stored per blood pump event.")]
	private float bloodReagentStoreAmount = 0.01f;

	[SerializeField]
	[Tooltip("Can we store any blood reagent?")]
	private float bloodReagentStoredMax = 1f;

	private float bloodReagentStored = 0;

	private void Awake()
	{
		attributes = GetComponent<ItemAttributesV2>();
	}

	public virtual void ImplantUpdate(LivingHealthMasterBase healthMaster)
	{
		foreach (ImplantProperty prop in properties)
		{
			prop.ImplantUpdate(this, healthMaster);
		}
	}

	public float BloodPumpedEvent(Chemistry.Reagent bloodReagent, float amountOfBloodReagentPumped)
	{
		float returnedReagent = amountOfBloodReagentPumped;

		//If no oxygenated blood is getting pumped, check our stores.
		if (returnedReagent <= 0)
		{
			if (bloodReagentStored > 0)
			{
				bloodReagentStored -= bloodReagentConsumed;
			}

			return returnedReagent;
		}

		if (isBloodReagentConsumed)
		{
			returnedReagent -= bloodReagentConsumed;

			if (returnedReagent > 0 && bloodReagentStored < bloodReagentStoredMax)
			{
				bloodReagentStored += bloodReagentExcessConsumption;
				returnedReagent -= bloodReagentExcessConsumption;
			}
		}
		return returnedReagent;

	}
}
