using System.Collections;
using System.Collections.Generic;
using HealthV2.Living.Surgery;
using UnityEngine;

namespace HealthV2
{
	[CreateAssetMenu(fileName = "RemovalProcedure", menuName = "ScriptableObjects/Surgery/RemovalProcedure")]
	public class RemovalProcedure : SurgeryProcedureBase
	{
		public override void FinnishSurgeryProcedure(BodyPart OnBodyPart, HandApply interaction,
			PresentProcedure presentProcedure)
		{
			base.FinnishSurgeryProcedure(OnBodyPart, interaction, presentProcedure);
			if (presentProcedure.RelatedBodyPart.ContainedIn != null)
			{
				presentProcedure.isOn.SetBodyPartIsOpen(false,true) ;
				presentProcedure.isOn.currentlyOn = presentProcedure.RelatedBodyPart.ContainedIn.gameObject;
			}
			else
			{
				presentProcedure.isOn.SetBodyPartIsOpen(false,false) ;
				presentProcedure.isOn.currentlyOn = null;
			}

			presentProcedure.isOn.ThisPresentProcedure.PreviousBodyPart = null;
			presentProcedure.isOn.ThisPresentProcedure.RelatedBodyPart = null;

			OnBodyPart.TryRemoveFromBody();
		}
	}
}