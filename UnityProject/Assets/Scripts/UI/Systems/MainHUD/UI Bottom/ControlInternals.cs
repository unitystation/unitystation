using System;
using System.Collections.Generic;
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

	private GameObject Mask;
	private GameObject Tank;

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
					Logger.LogError("Internals state is out of range. <1; 5>", Category.PlayerInventory);
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
		EventManager.AddHandler(Event.EnableInternals, OnEnableInternals);
		EventManager.AddHandler(Event.DisableInternals, OnDisableInternals);
	}

	void OnDisable()
	{
		EventManager.RemoveHandler(Event.EnableInternals, OnEnableInternals);
		EventManager.RemoveHandler(Event.DisableInternals, OnDisableInternals);
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

		SoundManager.Play(CommonSounds.Instance.Click01);

		if (isAirflowEnabled)
			EventManager.Broadcast(Event.DisableInternals);
		else
			EventManager.Broadcast(Event.EnableInternals);

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
		PlayerManager.LocalPlayerScript.DynamicItemStorage.OnContentsChangeClient.AddListener(InventoryChange);
	}

	public void InventoryChange()
	{
		if (PlayerManager.LocalPlayerScript.IsGhost) return;
		if (Mask == null)
		{
			foreach (var maskItemSlot in PlayerManager.LocalPlayerScript.DynamicItemStorage.GetNamedItemSlots(NamedSlot.mask ))
			{
				if (maskItemSlot.ItemObject != null && maskItemSlot.ItemAttributes != null)
				{
					if (maskItemSlot.ItemAttributes.CanConnectToTank)
					{
						Mask = maskItemSlot.ItemObject;
						isWearingMask = true;
						break;
					}
				}
			}
		}

		if (Tank == null)
		{
			bool Doublebreak = false;
			foreach (NamedSlot namedSlot in DynamicItemStorage.GasUseSlots)
			{
				foreach (ItemSlot itemSlot in PlayerManager.LocalPlayerScript.DynamicItemStorage.GetNamedItemSlots(namedSlot))
				{
					if (itemSlot.ItemObject != null && itemSlot.ItemObject.TryGetComponent(out GasContainer gasContainer))
					{
						Tank = itemSlot.ItemObject;
						this.gasContainer = gasContainer;
						Doublebreak = true;
						break;
					}
				}
				if (Doublebreak) break;
			}
		}


		if (Mask != null)
		{
			if (PlayerManager.LocalPlayerScript.DynamicItemStorage.InventoryHasObject(Mask) == false)
			{
				isWearingMask = false;
				Mask = null;
			}
		}

		if (Tank != null)
		{
			if (PlayerManager.LocalPlayerScript.DynamicItemStorage.InventoryHasObject(Tank) == false)
			{
				gasContainer = null;
				Tank = null;
			}
		}
		UpdateState();
	}


	private void Update()
	{
		if (gasContainer != null && airTankFillImage != null)
		{
			airTankFillImage.fillAmount = gasContainer.FullPercentageClient;
		}
	}

	private void UpdateState()
	{
		// Player is wearing neither a tank nor a mask
		if (!isWearingMask && gasContainer == null)
		{
			CurrentState = 1;
			if(isAirflowEnabled)
				EventManager.Broadcast(Event.DisableInternals);
		}
		// Player is wearing a tank, but no mask.
		else if (!isWearingMask && gasContainer != null)
		{
			CurrentState = 2;
			if(isAirflowEnabled)
				EventManager.Broadcast(Event.DisableInternals);
		}
		// Player is wearing a mask, but no tank
		else if (isWearingMask && gasContainer == null)
		{
			CurrentState = 3;
			if(isAirflowEnabled)
				EventManager.Broadcast(Event.DisableInternals);
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
