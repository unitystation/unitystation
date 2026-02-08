using UnityEngine;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.HealthV2.Living.CirculatorySystem;

namespace US13.HealthV2.Living.Surgery.Procedures
{
	[CreateAssetMenu(fileName = "CloseProcedure", menuName = "ScriptableObjects/Surgery/CloseProcedure")]
	public class CloseProcedure : SurgeryProcedureBase
	{
		public override void FinnishSurgeryProcedure(BodyPart OnBodyPart, HandApply interaction,
			PresentProcedure presentProcedure)
		{
			base.FinnishSurgeryProcedure(OnBodyPart, interaction, presentProcedure);
			if (presentProcedure.RelatedBodyPart.ContainedIn != null && presentProcedure.RelatedBodyPart.ContainedIn.IsOpenAir == false)
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