using System;
using Objects.Atmospherics;
using Systems.Atmospherics;
using Items;
using UnityEngine;
using UnityEngine.UI;

public class ControlInternals : TooltipMonoBehaviour
{
	[SerializeField] private Image airTank = default; // TODO: unused and is creating a compiler warning.
	[SerializeField] private Image airTankFillImage = default;
	[SerializeField] private Image mask = default;

	[Header("Color settings")]
	[SerializeField] private Color activeAirFlowTankColor = default;

	[NonSerialized] private int _currentState = 1;
	public int CurrentState
	{
		get => _currentState;
		set
		{
			_currentState = value;
			switch (_currentState)
			{
				case 1:
					// Player is wearing neither a tank nor a mask.
					// No tubes and mask are shown in HUD, the air tank is greyed out.
					mask.color = Color.white;
					mask.enabled = false;

					airTankFillImage.fillAmount = 0;
					break;
				case 2:
					// Player is wearing a tank, but no mask.
					// The tank icon now shows the fullness/capacity of the tank, but no tubes/mask are shown
					mask.color = Color.white;
					mask.enabled = false;
					break;
				case 3:
					// Player is wearing a mask, but no tank.
					// Tubes and mask are shown in the HUD, but the tank remains greyed out.
					mask.color = Color.white;
					mask.enabled = true;

					airTankFillImage.fillAmount = 0;
					break;
				case 4:
					// Player is wearing a mask and a tank, but airflow is off.
					// The air tank is shown in blue in the HUD, and the mask is shown too.
					mask.color = Color.white;
					mask.enabled = true;
					break;
				case 5:
					// Player is wearing a mask and tank, and the airflow is on.
					// Same as above, but the air tank in the HUD is given a green hue to show the difference.
					mask.color = activeAirFlowTankColor;
					mask.enabled = true;
					break;
				default:
					Logger.LogError("currentState is out of range. <1; 5>");
					break;
			}
		}
	}
	public bool isAirflowEnabled;
	private GasContainer gasContainer = null;
	private bool isWearingMask = false;

	public override string Tooltip => "toggle air flow";

	void Awake()
	{
		isAirflowEnabled = false;
	}

	void OnEnable()
	{
		EventManager.AddHandler(EVENT.EnableInternals, OnEnableInternals);
		EventManager.AddHandler(EVENT.DisableInternals, OnDisableInternals);
	}

	void OnDisable()
	{
		EventManager.RemoveHandler(EVENT.EnableInternals, OnEnableInternals);
		EventManager.RemoveHandler(EVENT.DisableInternals, OnDisableInternals);

		if (PlayerManager.LocalPlayerScript != null && PlayerManager.LocalPlayerScript.IsGhost == false)
		{
			RemoveListeners();
		}
	}

	/// <summary>
	/// toggle the button state and play any sounds
	/// </summary>
	public void OxygenSelect()
	{
		if (CurrentState != 4 && CurrentState != 5)
			return;

		if (PlayerManager.LocalPlayer == null)
			return;

		if (PlayerManager.LocalPlayerScript.playerHealth.IsCrit)
			return;

		SoundManager.Play(SingletonSOSounds.Instance.Click01);

		if (isAirflowEnabled)
			EventManager.Broadcast(EVENT.DisableInternals);
		else
			EventManager.Broadcast(EVENT.EnableInternals);

		UpdateState();
	}

	public void OnEnableInternals()
	{
		isAirflowEnabled = true;
	}

	public void OnDisableInternals()
	{
		isAirflowEnabled = false;
	}

	public void SetupListeners()
	{
		UpdateState();

		ItemSlot maskSlot = PlayerManager.LocalPlayerScript.ItemStorage.GetNamedItemSlot(NamedSlot.mask);
		maskSlot.OnSlotContentsChangeClient.AddListener(() => OnMaskChanged(maskSlot));

		foreach (NamedSlot namedSlot in ItemStorage.GasUseSlots)
		{
			ItemSlot itemSlot = PlayerManager.LocalPlayerScript.ItemStorage.GetNamedItemSlot(namedSlot);
			itemSlot.OnSlotContentsChangeClient.AddListener(() => OnOxygenTankEquipped());
		}
	}

	public void RemoveListeners()
	{
		ItemSlot maskSlot = PlayerManager.LocalPlayerScript.ItemStorage.GetNamedItemSlot(NamedSlot.mask);
		maskSlot.OnSlotContentsChangeClient.RemoveListener(() => OnMaskChanged(maskSlot));

		foreach (NamedSlot namedSlot in ItemStorage.GasUseSlots)
		{
			ItemSlot itemSlot = PlayerManager.LocalPlayerScript.ItemStorage.GetNamedItemSlot(namedSlot);
			itemSlot.OnSlotContentsChangeClient.RemoveListener(() => OnOxygenTankEquipped());
		}
	}

	private void OnMaskChanged(ItemSlot itemSlot)
	{
		ItemAttributesV2 maskItemAttrs = itemSlot.ItemAttributes;
		if (maskItemAttrs != null && maskItemAttrs.CanConnectToTank)
			isWearingMask = true;
		else
			isWearingMask = false;

		UpdateState();
	}

	private void OnOxygenTankEquipped()
	{
		this.gasContainer = null;
		foreach (NamedSlot namedSlot in ItemStorage.GasUseSlots)
		{
			ItemSlot itemSlot = PlayerManager.LocalPlayerScript.ItemStorage.GetNamedItemSlot(namedSlot);
			if (itemSlot.ItemObject != null && itemSlot.ItemObject.TryGetComponent(out GasContainer gasContainer))
			{
				this.gasContainer = gasContainer;
				break;
			}
		}

		UpdateState();
	}

	private void Update()
	{
		if (gasContainer != null)
		{
			airTankFillImage.fillAmount = gasContainer.GasMix.GetMoles(Gas.Oxygen) / gasContainer.MaximumMoles;
		}
	}

	private void UpdateState()
	{
		// Player is wearing neither a tank nor a mask
		if (!isWearingMask && gasContainer == null)
		{
			CurrentState = 1;
			if(isAirflowEnabled)
				EventManager.Broadcast(EVENT.DisableInternals);
		}
		// Player is wearing a tank, but no mask.
		else if (!isWearingMask && gasContainer != null)
		{
			CurrentState = 2;
			if(isAirflowEnabled)
				EventManager.Broadcast(EVENT.DisableInternals);
		}
		// Player is wearing a mask, but no tank
		else if (isWearingMask && gasContainer == null)
		{
			CurrentState = 3;
			if(isAirflowEnabled)
				EventManager.Broadcast(EVENT.DisableInternals);
		}
		// Player is wearing a mask and a tank, but airflow is off
		else if (isWearingMask && gasContainer != null && !isAirflowEnabled)
		{
			CurrentState = 4;
		}
		// Player is wearing a mask and tank, and the airflow is on
		else if (isWearingMask && gasContainer != null && isAirflowEnabled)
		{
			CurrentState = 5;
		}
	}
}
