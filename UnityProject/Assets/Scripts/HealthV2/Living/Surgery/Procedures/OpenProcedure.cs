﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HealthV2
{
	[CreateAssetMenu(fileName = "OpenProcedure", menuName = "ScriptableObjects/Surgery/OpenProcedure")]
	public class OpenProcedure : SurgeryProcedureBase
	{
		public override void FinnishSurgeryProcedure(BodyPart OnBodyPart, PositionalHandApply interaction,
			Dissectible.PresentProcedure PresentProcedure)
		{
			base.FinnishSurgeryProcedure(OnBodyPart, interaction, PresentProcedure);
			PresentProcedure.ISon.SetBodyPartIsOpen(true,true);

		}

		public override void UnsuccessfulStep(BodyPart OnBodyPart, PositionalHandApply interaction,
			Dissectible.PresentProcedure PresentProcedure)
		{
			base.UnsuccessfulStep(OnBodyPart, interaction,PresentProcedure );
		}
	}
}