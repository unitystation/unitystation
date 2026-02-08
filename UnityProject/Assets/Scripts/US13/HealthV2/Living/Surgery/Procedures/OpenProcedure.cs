using UnityEngine;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.HealthV2.Living.CirculatorySystem;

namespace US13.HealthV2.Living.Surgery.Procedures
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