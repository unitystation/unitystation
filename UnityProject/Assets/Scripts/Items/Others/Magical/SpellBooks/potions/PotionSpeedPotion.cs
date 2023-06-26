using System.Collections;
using System.Collections.Generic;
using Clothing;
using Items;
using UnityEngine;

public class PotionSpeedPotion : MonoBehaviour, ICheckedInteractable<HandApply>
{

	public Color PotionColour = new Color(1f, 0.992156f, 0.0039f);

	private static readonly StandardProgressActionConfig ProgressConfig =
		new StandardProgressActionConfig(StandardProgressActionType.SelfHeal);

	public virtual bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (DefaultWillInteract.Default(interaction, side) == false) return false;
		return interaction.Intent == Intent.Help;
	}

	public virtual void ServerPerformInteraction(HandApply interaction)
	{
		if (CheckTarget(interaction.TargetObject))
		{
			void ProgressComplete()
			{
				ServerApplyPotion(interaction.TargetObject);
			}

			StandardProgressAction.Create(ProgressConfig, ProgressComplete)
				.ServerStartProgress(interaction.Performer.RegisterTile(), 5f,
					interaction.Performer); //TODO Think about
		}

	}


	public void ServerApplyPotion(GameObject Target)
	{
		if (CheckTarget(Target))
		{
			var WearableSpeedDebuff = Target.GetComponent<WearableSpeedDebuff>();

			WearableSpeedDebuff.SpeedDebuffRemoved = true;

			var Sprites = Target.GetComponentsInChildren<SpriteHandler>();

			foreach (var Sprite in Sprites)
			{
				Sprite.SetColor(PotionColour);
			}

			_ = Despawn.ServerSingle(this.gameObject);
		}
	}


	public bool CheckTarget(GameObject Target)
	{
		var WearableSpeedDebuff = Target.GetComponent<WearableSpeedDebuff>();
		if (WearableSpeedDebuff == null) return false;
		if (WearableSpeedDebuff.SpeedDebuffRemoved) return false;
		return true;

	}
}
