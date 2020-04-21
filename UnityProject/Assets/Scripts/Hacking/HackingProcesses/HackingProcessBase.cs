using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using Mirror;


/// <summary>
/// This is a controller for hacking an object. This compoenent being attached to an object means that the object is hackable.
/// It will check interactions with the object, and once the goal interactions have been met, it will open a hacking UI prefab.
/// e.g. check if interacted with a screw driver, then check if 
/// </summary>
public abstract class HackingProcessBase : NetworkBehaviour, IPredictedCheckedInteractable<HandApply>, IServerSpawn
{
	[SerializeField]
	[Tooltip("The prefab spawned when interacting with exposed wires. Should be a UI element.")]
	private GameObject hackingPrefab;

	[SerializeField]
	[Tooltip("Whether the wires used to hack the object are initially exposed when the object is spawned.")]
	private bool wiresInitiallyExposed = false;

	[SyncVar(hook = nameof(SyncWiresExposed))]
	private bool wiresExposed = false;
	public bool WiresExposed => wiresExposed; //Public wrapper for use outside the class.

	//The hacking GUI that is registered to this component.
	private GUI_Hacking hackingGUI;

	[SerializeField]
	[Tooltip("What the initial stage of the hack should be when the object is spawned.")]
	private int hackInitialStage = 0;
	/// <summary>
	/// This is a convenience function. Since some devices need to have several steps be completed in order to expose their wiring, this just adds a simple way of
	/// communicating between server/client what stage of the hack we're up to. Saves having to recreate it each time we make a new hacking process.
	/// </summary>
	[SyncVar(hook = nameof(SyncHackStage))]
	private int hackStage = 0;
	public int HackStage => hackStage;

	public override void OnStartClient()
	{
		SyncWiresExposed(wiresExposed, wiresExposed);
	}

	public void OnSpawnServer(SpawnInfo info)
	{
		SyncWiresExposed(wiresInitiallyExposed, wiresInitiallyExposed);
	}

	protected void SyncWiresExposed(bool _oldWiresExposed, bool _newWiresExposed)
	{
		wiresExposed = _newWiresExposed;
		if (_newWiresExposed)
		{
			OnWiresExposed();
		}
		else
		{
			OnWiresHidden();
		}
	}

	protected void ToggleWiresExposed()
	{
		SyncWiresExposed(wiresExposed, !wiresExposed);
	}

	protected void SyncHackStage(int _oldStage, int _newStage)
	{
		hackStage = _newStage;
		OnHackStageSet(_oldStage, _newStage);
	}

	public void RegisterHackingGUI(GUI_Hacking hackUI)
	{
		hackingGUI = hackUI;
	}

	//These sounds are used when the security panel on the object is opened.
	public string openPanelSFX = null, closePanelSFX = null;

	public abstract void ClientPredictInteraction(HandApply interaction);

	public abstract void ServerPerformInteraction(HandApply interaction);

	public abstract void ServerRollbackClient(HandApply interaction);

	public abstract bool WillInteract(HandApply interaction, NetworkSide side);

	/// <summary>
	/// This creates the UI prefab used for hacking this object.
	/// </summary>
	public abstract void CreateHackPrefab();

	/// <summary>
	/// These functions are called when the SyncVars are set using the appropriate hooks.
	/// DO NOT CALL THESE ELSEWHERE!
	/// </summary>
	protected virtual void OnWiresExposed() { }

	protected virtual void OnWiresHidden() { }

	protected virtual void OnHackStageSet(int oldStage, int newStage) { }

}
