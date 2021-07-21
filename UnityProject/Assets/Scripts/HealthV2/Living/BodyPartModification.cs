using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HealthV2
{
	public class BodyPartModification : MonoBehaviour
	{
		[NonSerialized]
		public BodyPart RelatedPart;
		public virtual void ImplantPeriodicUpdate(){} 
		public virtual void RemovedFromBody(LivingHealthMasterBase livingHealthMasterBase){}
		public virtual void HealthMasterSet(){}
		public virtual void SetUpSystems(){}

		public virtual void Initialisation(){}
		public virtual void BloodWasPumped(){}
		public virtual void InternalDamageLogic()
		{
			RelatedPart.InternalBleedingLogic();
		}

		/// <summary>
		/// Whenever a bodyPart breaks this logic is called to display information to players and add in debuffs.
		/// By default this function's base only handles chat messages.
		/// </summary>
		/// <param name="stage"></param>
		public virtual void BodyPartBreakLogic(BodyPart.BluntDamageLevels stage)
		{
			string txtToPlayers;
			switch (stage)
			{
				case BodyPart.BluntDamageLevels.NONE:
					break;
				case BodyPart.BluntDamageLevels.JointDislocation:
					txtToPlayers = RelatedPart.TranslateTextTags(RelatedPart.BodyPartBreakVisibleTextOnSTAGEONE);
					Chat.AddActionMsgToChat(RelatedPart.HealthMaster.gameObject, txtToPlayers, txtToPlayers);
					break;
				case BodyPart.BluntDamageLevels.HairlineFracture:
					txtToPlayers = RelatedPart.TranslateTextTags(RelatedPart.BodyPartBreakVisibleTextOnSTAGETWO);
					Chat.AddActionMsgToChat(RelatedPart.HealthMaster.gameObject, txtToPlayers, txtToPlayers);
					break;
				case BodyPart.BluntDamageLevels.CompoundFracture:
					txtToPlayers = RelatedPart.TranslateTextTags(RelatedPart.BodyPartBreakVisibleTextOnSTAGETHREE);
					Chat.AddActionMsgToChat(RelatedPart.HealthMaster.gameObject, txtToPlayers, txtToPlayers);
					break;
			}
		}
		/// <summary>
		/// Logic for when a body part is broken and is getting an interaction.
		/// (I.E : Player drops items when picking up with broken arms.)
		/// </summary>
		public virtual void BodyPartBrokenInteractionLogic() { }
	}

}
