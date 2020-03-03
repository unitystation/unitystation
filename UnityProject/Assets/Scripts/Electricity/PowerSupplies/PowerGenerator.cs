using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
public class PowerGenerator : NetworkBehaviour, IInteractable<HandApply>, INodeControl
{
	private const float PlasmaConsumptionRate = 0.02f;

	public ObjectBehaviour objectBehaviour;
	[SyncVar(hook = nameof(UpdateSecured))]
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

	[SyncVar(hook = nameof(UpdateState))]
	public bool isOn = false;

	public ElectricalNodeControl ElectricalNodeControl;

	void Awake()
	{
		EnsureInit();
	}

	private void EnsureInit()
	{
		if (registerTile != null) return;
		registerTile = GetComponent<RegisterTile>();
	}

	public void PowerNetworkUpdate() { }

	public override void OnStartServer()
	{
		EnsureInit();
		UpdateSecured(isSecured, startSecured);
		StartCoroutine(CheckStartingPlasma());
	}

	/// <summary>
	/// Map solid plasma so that it is sitting on the same tile as the generator for it to be added
	/// to the starting plasma amounts.false Server Only.
	/// </summary>
	IEnumerator CheckStartingPlasma()
	{
		yield return WaitFor.Seconds(5); //Todo: figure out a robust way to init such things, don't rely on timeouts
		var plasmaObjs = registerTile.Matrix.Get<SolidPlasma>(registerTile.LocalPositionServer, true);
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
		EnsureInit();
		UpdateState(isOn, isOn);
	}

	public void UpdateState(bool wasOn, bool isOn)
	{
		EnsureInit();
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

	void UpdateSecured(bool wasSecured, bool _isSecured)
	{
		EnsureInit();
		isSecured = _isSecured;
		if (isServer)
		{
			objectBehaviour.ServerSetPushable(!isSecured);
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
		if (plasmaFuel.Count > 0)
		{
			plasmaFuel[0].StartBurningPlasma(PlasmaConsumptionRate, FuelExhaustedEvent);
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

	public void ServerPerformInteraction(HandApply interaction)
	{
		var slot = interaction.HandSlot;
		if (Validations.HasItemTrait(slot.ItemObject, CommonTraits.Instance.Wrench))
		{
			UpdateSecured(isSecured, !isSecured);
			ElectricalNodeControl.PowerUpdateStructureChange();
			if (!isSecured && isOn)
			{
				isOn = !isOn;
				UpdateServerState(isOn);
			}
			return;
		}

		var solidPlasma = slot.Item != null ? slot.Item.GetComponent<SolidPlasma>() : null;
		if (solidPlasma != null)
		{
			var plasma = Inventory.ServerVanishStackable(interaction.HandSlot);
			plasmaFuel.Add(plasma.GetComponent<SolidPlasma>());
			return;
		}

		if (isSecured)
		{
			UpdateServerState(!isOn);
		}
	}

}