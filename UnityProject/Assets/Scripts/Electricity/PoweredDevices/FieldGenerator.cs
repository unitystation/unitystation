using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class FieldGenerator : NetworkBehaviour, IInteractable<HandApply>, INodeControl
{
	public bool connectedToOther = false;
	private Coroutine coSpriteAnimator;
	public Sprite offSprite;
	public Sprite onSprite;
	public Sprite[] searchingSprites;
	public Sprite[] connectedSprites;
	public SpriteRenderer spriteRend;
	List<Sprite> animSprites = new List<Sprite>();
	public float Voltage;

	public ElectricalNodeControl ElectricalNodeControl;

	public ResistanceSourceModule ResistanceSourceModule;

	[SyncVar(hook = nameof(UpdateSprites))]
	public bool isOn = false;

	public override void OnStartClient()
	{
		UpdateSprites(isOn, isOn);
	}


	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;
		if (interaction.TargetObject != gameObject) return false;
		return true;
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		isOn = !isOn;
		UpdateSprites(isOn, isOn);
	}

	public void PowerNetworkUpdate()
	{
		Voltage = ElectricalNodeControl.Node.Data.ActualVoltage;
		UpdateSprites(isOn, isOn);
		//Logger.Log (Voltage.ToString () + "yeaahhh")   ;
	}

	void UpdateSprites(bool _wasOn, bool _isOn){
		isOn = _isOn;
		if (isOn)
		{
			if(Voltage < 2700){
				if (coSpriteAnimator != null) {
					StopCoroutine(coSpriteAnimator);
					coSpriteAnimator = null;
				}
				spriteRend.sprite = onSprite;
			}
			if(Voltage >= 2700){
				ResistanceSourceModule.Resistance = 50f;
				if(!connectedToOther){
					animSprites = new List<Sprite>(searchingSprites);
					if (coSpriteAnimator == null) {
						coSpriteAnimator = StartCoroutine(SpriteAnimator());
					}
				} else {
					animSprites = new List<Sprite>(connectedSprites);
					if(coSpriteAnimator == null) {
						coSpriteAnimator = StartCoroutine(SpriteAnimator());
					}
				}
			}
		}
		else
		{
			if (coSpriteAnimator != null)
			{
				StopCoroutine(coSpriteAnimator);
				coSpriteAnimator = null;
			}
			spriteRend.sprite = offSprite;
		}
	}
	//Check the operational state
	void CheckState(bool _isOn)
	{

	}

	IEnumerator SpriteAnimator()
	{
		int index = 0;
		while (true)
		{
			if (index >= animSprites.Count)
			{
				index = 0;
			}
			spriteRend.sprite = animSprites[index];
			index++;
			yield return WaitFor.Seconds(0.3f);
		}
	}
}