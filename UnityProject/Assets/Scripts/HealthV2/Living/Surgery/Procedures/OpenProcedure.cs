using System.Collections;
using System.Collections.Generic;
using HealthV2.Living.Surgery;
using UnityEngine;

namespace HealthV2
{
	[CreateAssetMenu(fileName = "OpenProcedure", menuName = "ScriptableObjects/Surgery/OpenProcedure")]
	public class OpenProcedure : SurgeryProcedureBase
	{
		public override void FinnishSurgeryProcedure(BodyPart OnBodyPart, HandApply interaction,
			PresentProcedure presentProcedure)
		{
			base.FinnishSurgeryProcedure(OnBodyPart, interaction, presentProcedure);
			presentProcedure.isOn.SetBodyPartIsOpen(true,true);

		}

		public override void UnsuccessfulStep(BodyPart OnBodyPart, HandApply interaction,
			PresentProcedure presentProcedure)
		{
			base.UnsuccessfulStep(OnBodyPart, interaction,presentProcedure );
		}
	}
}