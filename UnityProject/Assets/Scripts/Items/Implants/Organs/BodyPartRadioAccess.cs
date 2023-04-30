using System.Collections;
using System.Collections.Generic;
using HealthV2;
using UnityEngine;

public class BodyPartRadioAccess : BodyPartFunctionality
{
	[NaughtyAttributes.EnumFlags]
	public ChatChannel AvailableChannels;

	public override void OnRemovedFromBody(LivingHealthMasterBase livingHealth)
	{
		var CombinedRadioAccess = livingHealth.GetComponent<CombinedRadioAccess>();
		if (CombinedRadioAccess != null)
		{
			CombinedRadioAccess.RemoveAccess(this);
		}
	}

	public override void OnAddedToBody(LivingHealthMasterBase livingHealth)
	{
		var CombinedRadioAccess = livingHealth.GetComponent<CombinedRadioAccess>();
		if (CombinedRadioAccess != null)
		{
			CombinedRadioAccess.AddAccess(this);
		}
	} //Warning only add body parts do not remove body parts in this
}
