using System.Collections;
using System.Collections.Generic;
using HealthV2.Living.Surgery;
using UnityEngine;

namespace HealthV2
{
	[CreateAssetMenu(fileName = "CloseProcedure", menuName = "ScriptableObjects/Surgery/CloseProcedure")]
	public class CloseProcedure : SurgeryProcedureBase
	{
		public override void FinnishSurgeryProcedure(BodyPart OnBodyPart, HandApply interaction,
			PresentProcedure presentProcedure)
		{
			base.FinnishSurgeryProcedure(OnBodyPart, interaction, presentProcedure);
			if (presentProcedure.RelatedBodyPart.ContainedIn != null)
			{
				presentProcedure.isOn.currentlyOn = presentProcedure.RelatedBodyPart.ContainedIn.gameObject;
			}
			else
			{
				presentProcedure.isOn.SetBodyPartIsOpen(false,  false);
				presentProcedure.isOn.currentlyOn = null;
				presentProcedure.RelatedBodyPart = null;
			}
		}

		public override void UnsuccessfulStep(BodyPart OnBodyPart, HandApply interaction,
			PresentProcedure presentProcedure)
		{
			base.UnsuccessfulStep(OnBodyPart, interaction,presentProcedure );
		}
	}
}