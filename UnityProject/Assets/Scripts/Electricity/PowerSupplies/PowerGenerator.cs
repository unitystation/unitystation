using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
public class PowerGenerator : InputTrigger, INodeControl
{
	public ObjectBehaviour objectBehaviour;
	[SyncVar(hook = "UpdateSecured")]
	public bool isSecured; //To ground
	private RegisterTile registerTile;
	public bool startSecured;
	public bool startAsOn;
	public Sprite generatorSecuredSprite;
	public Sprite generatorOnSprite;
	public Sprite generatorUnSecuredSprite;
	public SpriteRenderer spriteRend;
	public AudioSource generatorRunSfx;
	public AudioSource generatorEndSfx;
	public ParticleSystem smokeParticles;
	//Server only
	public List<SolidPlasma> plasmaFuel = new List<SolidPlasma>();

	[SyncVar(hook = "UpdateState")]
	public bool isOn = false;

	public ElectricalNodeControl ElectricalNodeControl;

	void Awake()
	{
		registerTile = GetComponent<RegisterTile>();
	}

	public void PowerNetworkUpdate() { }

	public override void OnStartServer()
	{
		UpdateSecured(startSecured);
		StartCoroutine(CheckStartingPlasma());
	}

	/// <summary>
	/// Map solid plasma so that it is sitting on the same tile as the generator for it to be added
	/// to the starting plasma amounts.false Server Only.
	/// </summary>
	IEnumerator CheckStartingPlasma()
	{
		yield return YieldHelper.FiveSecs; //Todo: figure out a robust way to init such things, don't rely on timeouts
		var plasmaObjs = registerTile.Matrix.Get<SolidPlasma>(registerTile.PositionServer, true);
		foreach (SolidPlasma plasma in plasmaObjs)
		{
			plasmaFuel.Add(plasma);
			plasma.GetComponent<CustomNetTransform>().DisappearFromWorldServer();
		}

		if (startAsOn)
		{
			UpdateServerState(startAsOn);
		}
	}

	public override void OnStartClient()
	{
		base.OnStartClient();
		UpdateState(isOn);
	}

	public void UpdateState(bool isOn)
	{
		if (isOn)
		{
			generatorRunSfx.Play();
			spriteRend.sprite = generatorOnSprite;
			smokeParticles.Play();
		}
		else
		{
			generatorRunSfx.Stop();
			smokeParticles.Stop();
			if (isSecured)
			{
				generatorEndSfx.Play();
				spriteRend.sprite = generatorSecuredSprite;
			}
			else
			{
				spriteRend.sprite = generatorUnSecuredSprite;
			}
		}
	}

	public void UpdateServerState(bool _isOn)
	{
		if (_isOn)
		{
			if (TryBurnFuel())
			{
				ElectricalNodeControl.TurnOnSupply();
				isOn = true;
			}
		}
		else
		{
			isOn = false;
			ElectricalNodeControl.TurnOffSupply();
			if (plasmaFuel.Count > 0)
			{
				plasmaFuel[0].StopBurningPlasma();
			}
		}
	}

	void UpdateSecured(bool _isSecured)
	{
		isSecured = _isSecured;
		if (isServer)
		{
			objectBehaviour.isNotPushable = isSecured;
		}

		SoundManager.PlayAtPosition("Wrench", transform.position);

		if (!isSecured)
		{
			spriteRend.sprite = generatorUnSecuredSprite;
		}
		else
		{
			if (!isOn)
			{
				spriteRend.sprite = generatorSecuredSprite;
			}
			else
			{
				spriteRend.sprite = generatorOnSprite;
			}
		}
	}

	bool TryBurnFuel()
	{
		if (plasmaFuel.Count == 0)
		{
			return false;
		}

		if (plasmaFuel.Count > 0)
		{
			plasmaFuel[0].StartBurningPlasma(0.4f, FuelExhaustedEvent);
			return true;
		}
		return false;
	}

	//Server Only
	void FuelExhaustedEvent()
	{
		var pFuel = plasmaFuel[0];
		plasmaFuel.Remove(pFuel);
		if (isOn)
		{
			if (!TryBurnFuel())
			{
				UpdateServerState(false);
			}
		}
	}

	public override bool Interact(GameObject originator, Vector3 position, string hand)
	{
		if (!CanUse(originator, hand, position, false))
		{
			return false;
		}
		if (!isServer)
		{
			InteractMessage.Send(gameObject, hand);
		}
		else
		{
			var slot = InventoryManager.GetSlotFromOriginatorHand(originator, hand);
			var wrench = slot.Item?.GetComponent<WrenchTrigger>();
			if (wrench != null)
			{
				UpdateSecured(!isSecured);
				if (!isSecured && isOn)
				{
					isOn = !isOn;
					UpdateServerState(isOn);
				}
				return true;
			}

			var solidPlasma = slot.Item?.GetComponent<SolidPlasma>();
			if (solidPlasma != null)
			{
				plasmaFuel.Add(solidPlasma);
				InventoryManager.UpdateInvSlot(true, "", slot.Item, slot.UUID);
				return true;
			}

			if (isSecured)
			{
				UpdateServerState(!isOn);
			}
		}

		return true;
	}
}