﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HealthV2
{
	[CreateAssetMenu(fileName = "CloseProcedure", menuName = "ScriptableObjects/Surgery/CloseProcedure")]
	public class CloseProcedure : SurgeryProcedureBase
	{
		public override void FinnishSurgeryProcedure(BodyPart OnBodyPart, PositionalHandApply interaction,
			Dissectible.PresentProcedure PresentProcedure)
		{
			base.FinnishSurgeryProcedure(OnBodyPart, interaction, PresentProcedure);
			if (PresentProcedure.RelatedBodyPart.ContainedIn != null)
			{
				PresentProcedure.ISon.SetBodyPartIsOpen(false,  false);
				PresentProcedure.ISon.currentlyOn = PresentProcedure.RelatedBodyPart.ContainedIn.gameObject;
			}
			else
			{
				PresentProcedure.ISon.SetBodyPartIsOpen(false,  false);
				PresentProcedure.ISon.currentlyOn = null;
				PresentProcedure.RelatedBodyPart = null;
			}
		}

		public override void UnsuccessfulStep(BodyPart OnBodyPart, PositionalHandApply interaction,
			Dissectible.PresentProcedure PresentProcedure)
		{
			base.UnsuccessfulStep(OnBodyPart, interaction,PresentProcedure );
		}
	}
}