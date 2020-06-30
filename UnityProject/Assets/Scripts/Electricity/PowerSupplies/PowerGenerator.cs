using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
public class PowerGenerator : NetworkBehaviour, ICheckedInteractable<HandApply>, INodeControl
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
		yield return WaitFor.Seconds(1); //Todo: figure out a robust way to init such things, don't rely on timeouts
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
		if (_isOn && TryBurnFuel())
		{
			ElectricalNodeControl.TurnOnSupply();
			isOn = true;
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

		SoundManager.PlayAtPosition("Wrench", transform.position, gameObject);

		if (isSecured)
		{
			if (isOn)
			{
				spriteRend.sprite = generatorOnSprite;
			}
			else
			{
				spriteRend.sprite = generatorSecuredSprite;
			}
		}
		else
		{
			spriteRend.sprite = generatorUnSecuredSprite;
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
		if (plasmaFuel.Count > 0)
		{
			plasmaFuel.RemoveAt(0);
		}

		if (isOn)
		{
			if (!TryBurnFuel())
			{
				UpdateServerState(false);
			}
		}
	}

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;
		if (interaction.TargetObject != gameObject) return false;
		if (interaction.HandObject != null &&
		!Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Wrench) &&
		!Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.SolidPlasma)) return false;
		return true;
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Wrench))
		{
			UpdateSecured(isSecured, !isSecured);
			ElectricalManager.Instance.electricalSync.StructureChange = true;
			if (!isSecured && isOn)
			{
				isOn = !isOn;
				UpdateServerState(isOn);
			}
			return;
		}

		if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.SolidPlasma))
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