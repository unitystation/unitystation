using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FieldGenerator : MonoBehaviour, ICheckedInteractable<HandApply>, INodeControl
{
	private SpriteHandler spriteHandler;

	public ElectricalNodeControl ElectricalNodeControl;
	public ResistanceSourceModule ResistanceSourceModule;

	public bool connectedToOther = false;
	public float Voltage;
	public bool IsOn { get; private set; } = false;

	#region Lifecycle

	private void Awake()
	{
		spriteHandler = GetComponentInChildren<SpriteHandler>();
	}

	#endregion Lifecycle

	#region Interaction

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;
		if (interaction.TargetObject != gameObject) return false;
		if (interaction.HandObject != null) return false;

		return true;
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		TogglePower();
	}

	#endregion Interaction

	private void TogglePower()
	{
		IsOn = !IsOn;
		UpdateSprites();
	}

	public void PowerNetworkUpdate()
	{
		//Voltage = ElectricalNodeControl.Node.Data.ActualVoltage;
		//UpdateSprites(isOn, isOn);
		//Logger.Log (Voltage.ToString () + "yeaahhh")   ;
	}

	private void UpdateSprites()
	{
		if (IsOn)
		{
			if (Voltage < 2700)
			{
				spriteHandler.ChangeSprite((int) SpriteState.GeneratorOffBr);
			}
			else if (Voltage >= 2700)
			{
				ResistanceSourceModule.Resistance = 50f;
				if (!connectedToOther)
				{
					spriteHandler.ChangeSprite((int) SpriteState.GeneratorOnBr);
				}
				else
				{
					spriteHandler.ChangeSprite((int) SpriteState.GeneratorOn);
				}
			}
		}
		else
		{
			spriteHandler.ChangeSprite((int) SpriteState.GeneratorOff);
		}
	}

	//Check the operational state
	private void CheckState(bool _isOn)
	{

	}

	private enum SpriteState
	{
		GeneratorOff = 0,
		GeneratorOn = 1,
		GeneratorOffBr = 2,
		GeneratorOnBr = 3
	}
}
