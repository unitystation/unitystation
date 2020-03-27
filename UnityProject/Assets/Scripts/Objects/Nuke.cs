using System.Collections;
using UnityEngine;
using Mirror;
using UnityEngine.Events;
using UnityEngine.Serialization;

/// <summary>
/// Main component for nuke.
/// </summary>
public class Nuke : NetworkBehaviour, ICheckedInteractable<HandApply>, IAdminInfo
{
	private ObjectBehaviour objectBehaviour;
	private ItemStorage itemNuke;
	private ItemSlot nukeSlot;

	public ItemSlot NukeSlot
	{
		get { return nukeSlot; }
	}

	private bool isSafetyOn = true;

	public bool IsSafetyOn => isSafetyOn;

	private bool isCodeRight = false;

	public bool IsCodeRight => isCodeRight;

	public NukeTimerEvent OnTimerUpdate = new NukeTimerEvent();

	[SerializeField]
	private int minTimer = 270;
	private bool isTimer = false;

	public bool IsTimer => isTimer;

	private int currentTimerSeconds = 270;
	public int CurrentTimerSeconds
	{
		get => currentTimerSeconds;
		private set
		{
			currentTimerSeconds = value;
			OnTimerUpdate.Invoke(currentTimerSeconds);
		}
	}

	private Coroutine timerHandle;

	private CentComm.AlertLevel CurrentAlertLevel;

	public float cooldownTimer = 2f;
	public string interactionMessage;
	public string deniedMessage;
	private bool detonated = false;
	public bool IsDetonated => detonated;
	//Nuke code is only populated on the server
	private int nukeCode;
	public int NukeCode => nukeCode;

	private string currentCode = "";
	public string CurrentCode => currentCode;

	//	private GameObject Player;

	private void Awake()
	{
		objectBehaviour = GetComponent<ObjectBehaviour>();
		itemNuke = GetComponent<ItemStorage>();
		nukeSlot = itemNuke.GetIndexedItemSlot(0);
	}

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side))
			return false;

		//interaction only works if using an ID card on console
		if (!Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.NukeDisk))
		{ return false; }

		return true;
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		Inventory.ServerTransfer(interaction.HandSlot, nukeSlot);
	}

	public void EjectDisk()
	{
		if (!nukeSlot.IsEmpty)
		{
			Inventory.ServerDrop(nukeSlot);
			isCodeRight = false;
			Clear();
		}
	}

	public override void OnStartServer()
	{
		//calling the code generator and setting up a 10 second timer to post the code in syndicate chat
		CodeGenerator();
		base.OnStartServer();
	}

	/// <summary>
	/// Tries to add new digit to code input
	/// </summary>
	/// <param name="c"></param>
	/// <returns>true if digit is appended ok</returns>
	public bool AppendKey(char c) {
		int digit;
		if (int.TryParse(c.ToString(), out digit) && currentCode.Length < nukeCode.ToString().Length) {
			currentCode = CurrentCode + digit;
			return true;
		}
		return false;
	}

	IEnumerator WaitForDeath()
	{
		yield return WaitFor.Seconds(5f);
		GibMessage.Send();
		yield return WaitFor.Seconds(15f);
		// Trigger end of round
		GameManager.Instance.EndRound();
	}

	public bool? ToggleTimer()
	{
		if(IsCodeRight && !isSafetyOn)
		{
			if(isTimer)
			{
				GameManager.Instance.CentComm.ChangeAlertLevel(CurrentAlertLevel);
				this.TryStopCoroutine(ref timerHandle);
			}
			isTimer = !isTimer;
			return isTimer;
		}
		return null;
	}
	//Server validating the code sent back by the GUI
	[Server]
	public bool? Validate()
	{
		if(isCodeRight && isTimer)
		{
			int digit = int.Parse(currentCode);
			if(digit < minTimer)
			{
				return false;
			}
			CurrentTimerSeconds = digit;
			CurrentAlertLevel = GameManager.Instance.CentComm.CurrentAlertLevel;
			GameManager.Instance.CentComm.ChangeAlertLevel(CentComm.AlertLevel.Delta);
			StartCountDown();
			return true;
		}
		if (!isCodeRight)
		{
			isCodeRight = CurrentCode == NukeCode.ToString() ? true : false;
			return isCodeRight;
		}
		return null;
	}

	[Server]
	public void StartCountDown()
	{
		
		this.StartCoroutine(TickTimer(), ref timerHandle);	
	}
	[Server]
	private void Detonate()
	{
		SoundManager.PlayNetworked("NukeDetonate");

		detonated = true;
		//if yes, blow up the nuke
		RpcDetonate();
		//Kill Everyone in the universe
		//FIXME kill only people on the station matrix that the nuke was detonated on
		StartCoroutine(WaitForDeath());
		GameManager.Instance.RespawnCurrentlyAllowed = false;
	}
	private IEnumerator TickTimer()
	{
		while(CurrentTimerSeconds > 0)
		{
			CurrentTimerSeconds -= 1;
			SoundManager.PlayAtPosition("TimerTick", gameObject.AssumedWorldPosServer());
			yield return WaitFor.Seconds(1);
		}
		Detonate();
	}

	//Server telling the nukes to explode
	[ClientRpc]
	void RpcDetonate()
	{
		if(detonated){
			return;
		}
		detonated = true;

		SoundManager.StopAmbient();
		//turning off all the UI except for the right panel
		UIManager.PlayerHealthUI.gameObject.SetActive(false);
		UIManager.Display.hudBottomHuman.gameObject.SetActive(false);
		UIManager.Display.hudBottomGhost.gameObject.SetActive(false);
		ChatUI.Instance.CloseChatWindow();

		//Playing the video
		UIManager.Display.VideoPlayer.PlayNukeDetVideo();
	}

	[Server]
	public void CodeGenerator()
	{
		nukeCode = Random.Range(1000, 9999);
		Debug.Log("NUKE CODE: " + nukeCode + " POS: " + transform.position);
	}

	public void Clear() {
		currentCode = "";
	}

	[Server]
	public bool? SafetyNuke()
	{
		if(!isSafetyOn)
		{
			if (isTimer)
			{
				GameManager.Instance.CentComm.ChangeAlertLevel(CurrentAlertLevel);
				isTimer = false;
				this.TryStopCoroutine(ref timerHandle);
			}
			isSafetyOn = !isSafetyOn;
			return isSafetyOn;
		}
		else if(isCodeRight)
		{
			isSafetyOn = !isSafetyOn;
			return isSafetyOn;
		}
		return null;
	}

	[Server]
	public bool? AnchorNuke()
	{
		if (IsCodeRight && !isSafetyOn)
		{
			bool isPushable = !objectBehaviour.IsPushable;
			GetComponent<ObjectBehaviour>().ServerSetPushable(isPushable);
			return isPushable;
		}
		return null;
	}


	public string AdminInfoString()
	{
		return $"Nuke Code: {nukeCode}";
	}

}
public class NukeTimerEvent : UnityEvent<int> { }