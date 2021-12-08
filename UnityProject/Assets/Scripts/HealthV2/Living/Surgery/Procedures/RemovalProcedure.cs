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
				PresentProcedure.ISon.SetBodyPartIsOpen(false,true) ;
				PresentProcedure.ISon.currentlyOn = PresentProcedure.RelatedBodyPart.ContainedIn.gameObject;
			}
			else
			{
				PresentProcedure.ISon.SetBodyPartIsOpen(false,false) ;
				PresentProcedure.ISon.currentlyOn = null;
			}

			PresentProcedure.ISon.ThisPresentProcedure.PreviousBodyPart = null;
			PresentProcedure.ISon.ThisPresentProcedure.RelatedBodyPart = null;

			OnBodyPart.TryRemoveFromBody();
		}
	}
}