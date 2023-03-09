using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HealthV2
{
	[CreateAssetMenu(fileName = "RemovalProcedure", menuName = "ScriptableObjects/Surgery/RemovalProcedure")]
	public class RemovalProcedure : SurgeryProcedureBase
	{
		public override void FinnishSurgeryProcedure(BodyPart OnBodyPart, HandApply interaction,
			Dissectible.PresentProcedure PresentProcedure)
		{
			base.FinnishSurgeryProcedure(OnBodyPart, interaction, PresentProcedure);
			if (PresentProcedure.RelatedBodyPart.ContainedIn != null)
			{
				PresentProcedure.isOn.SetBodyPartIsOpen(false,true) ;
				PresentProcedure.isOn.currentlyOn = PresentProcedure.RelatedBodyPart.ContainedIn.gameObject;
			}
			else
			{
				PresentProcedure.isOn.SetBodyPartIsOpen(false,false) ;
				PresentProcedure.isOn.currentlyOn = null;
			}

			PresentProcedure.isOn.ThisPresentProcedure.PreviousBodyPart = null;
			PresentProcedure.isOn.ThisPresentProcedure.RelatedBodyPart = null;

			OnBodyPart.TryRemoveFromBody();
		}
	}
}