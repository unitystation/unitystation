using System;
using TMPro;
using UnityEngine;
using US13.Core.Sprite_Handler;
using US13.HealthV2.Living.BodyParts;
using US13.HealthV2.Living.CirculatorySystem;

namespace US13.HealthV2.Living.Surgery
{
	public class SurgicalProcessItem : MonoBehaviour
	{
		public SpriteHandler OrganSprite;
		public SpriteHandler OperationSprite;
		public TMP_Text TitleText;

		public Action ToPerform;

		public void BodyToChoose(BodyPart bodyPart, Action inAction, SpriteDataSO InOperationSprite, string Operation)
		{
			var Sprite = bodyPart.GetComponentInChildren<SpriteHandler>();
			if (Sprite != null && Sprite.GetCurrentSpriteSO() != null)
			{
				OrganSprite.SetSpriteSO(Sprite.GetCurrentSpriteSO());
			}

			OperationSprite.SetSpriteSO(InOperationSprite);

			TitleText.text = Operation + " " +  bodyPart.name;

			ToPerform = inAction;
		}

		public void ProcedureToChoose(GameObject bodyPart, Action inAction, SpriteDataSO InOperationSprite, string Operation)
		{
			var Sprite = bodyPart.GetComponentInChildren<SpriteHandler>();
			if (Sprite != null && Sprite.GetCurrentSpriteSO() != null)
			{
				OrganSprite.SetSpriteSO(Sprite.GetCurrentSpriteSO());
			}

			OperationSprite.SetSpriteSO(InOperationSprite);

			TitleText.text = Operation + " " +  bodyPart.name;

			ToPerform = inAction;
		}


		public void TriggerAction()
		{
			ToPerform?.Invoke();
		}
	}
}