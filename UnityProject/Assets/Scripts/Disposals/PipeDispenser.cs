using System.Collections;
using UnityEngine;
using Mirror;

public class PipeDispenser : NetworkBehaviour
{
	const float ANIMATION_TIME = 2; // As per sprite sheet JSON file.

	ObjectBehaviour objectBehaviour;
	WrenchSecurable securable;
	HasNetworkTab netTab;
	SpriteHandler spriteHandler;

	Coroutine animationRoutine;

	bool machineOperating = false;

	public bool MachineOperating => machineOperating;

	[SyncVar(hook = nameof(SyncObjectProperties))]
	PipeObjectSettings newPipe;

	public enum PipeLayer
	{
		LayerOne,
		LayerTwo,
		LayerThree
	}

	void Awake()
	{
		objectBehaviour = GetComponent<ObjectBehaviour>();
		securable = GetComponent<WrenchSecurable>();
		netTab = GetComponent<HasNetworkTab>();
		spriteHandler = transform.GetChild(0).GetComponent<SpriteHandler>();

		securable.OnAnchoredChange.AddListener(OnAnchoredChange);
	}

	void OnMachineStatusChange(bool oldState, bool newState)
	{
		machineOperating = newState;
		UpdateSprite();
	}

	void UpdateSprite()
	{
		if (MachineOperating) spriteHandler.ChangeSprite(1);
		else spriteHandler.ChangeSprite(0);
	}

	void SyncObjectProperties(PipeObjectSettings oldState, PipeObjectSettings newState)
	{
		newPipe = newState;
		newPipe.pipeObject.GetComponentInChildren<SpriteRenderer>().color = newPipe.pipeColor;
	}

	public void Dispense(GameObject objectPrefab, PipeLayer pipeLayer, Color pipeColor)
	{
		if (MachineOperating || !securable.IsAnchored) return;

		this.RestartCoroutine(SetMachineOperating(), ref animationRoutine);
		SpawnResult spawnResult = Spawn.ServerPrefab(objectPrefab, objectBehaviour.AssumedWorldPositionServer());
		if (spawnResult.Successful)
		{
			newPipe = new PipeObjectSettings {
					pipeObject = spawnResult.GameObject,
					pipeColor = pipeColor
			};
		}
		else
		{
			throw new MissingReferenceException(
					$"Failed to spawn an object from {name}! Is GUI_{name} missing reference to object prefab?");
		}
	}

	IEnumerator SetMachineOperating()
	{
		machineOperating = true;
		UpdateSprite();
		yield return WaitFor.Seconds(ANIMATION_TIME);
		machineOperating = false;
		UpdateSprite();
	}

	void OnAnchoredChange()
	{
		netTab.enabled = securable.IsAnchored;
	}

	struct PipeObjectSettings
	{
		public GameObject pipeObject;
		public Color pipeColor;
	}
}
