using System;
using System.Collections;
using System.Collections.Generic;
using HealthV2;
using TMPro;
using UnityEngine;

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