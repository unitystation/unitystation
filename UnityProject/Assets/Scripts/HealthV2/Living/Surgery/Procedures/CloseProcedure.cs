using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HealthV2
{
	[CreateAssetMenu(fileName = "CloseProcedure", menuName = "ScriptableObjects/Surgery/CloseProcedure")]
	public class CloseProcedure : SurgeryProcedureBase
	{
		public override void FinnishSurgeryProcedure(BodyPart OnBodyPart, HandApply interaction,
			Dissectible.PresentProcedure PresentProcedure)
		{
			base.FinnishSurgeryProcedure(OnBodyPart, interaction, PresentProcedure);
			if (PresentProcedure.RelatedBodyPart.ContainedIn != null)
			{
				PresentProcedure.isOn.currentlyOn = PresentProcedure.RelatedBodyPart.ContainedIn.gameObject;
			}
			else
			{
				PresentProcedure.isOn.SetBodyPartIsOpen(false,  false);
				PresentProcedure.isOn.currentlyOn = null;
				PresentProcedure.RelatedBodyPart = null;
			}
		}

		public override void UnsuccessfulStep(BodyPart OnBodyPart, HandApply interaction,
			Dissectible.PresentProcedure PresentProcedure)
		{
			base.UnsuccessfulStep(OnBodyPart, interaction,PresentProcedure );
		}
	}
}