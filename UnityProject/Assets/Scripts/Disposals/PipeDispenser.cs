using System.Collections;
using UnityEngine;
using Mirror;
using Pipes;

public class PipeDispenser : NetworkBehaviour
{
	const float DISPENSING_TIME = 2; // As per sprite sheet JSON file.

	ObjectBehaviour objectBehaviour;
	WrenchSecurable securable;
	HasNetworkTab netTab;
	SpriteHandler spriteHandler;

	Coroutine animationRoutine;

	public bool MachineOperating { get; private set; } = false;

	[SyncVar(hook = nameof(SyncObjectProperties))]
	PipeObjectSettings newPipe;

	public enum PipeLayer
	{
		LayerOne,
		LayerTwo,
		LayerThree
	}

	private enum SpriteState
	{
		Idle = 0,
		Operating = 1
	}

	void Awake()
	{
		objectBehaviour = GetComponent<ObjectBehaviour>();
		securable = GetComponent<WrenchSecurable>();
		netTab = GetComponent<HasNetworkTab>();
		spriteHandler = transform.GetChild(0).GetComponent<SpriteHandler>();

		securable.OnAnchoredChange.AddListener(OnAnchoredChange);
	}

	void UpdateSprite()
	{
		if (MachineOperating)
		{
			spriteHandler.ChangeSprite((int) SpriteState.Operating);
		}
		else
		{
			spriteHandler.ChangeSprite((int) SpriteState.Idle);
		}
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
			spawnResult.GameObject.GetComponent<PipeItem>()?.SetColour(pipeColor);

			newPipe = new PipeObjectSettings
			{
					pipeObject = spawnResult.GameObject,
					pipeColor = pipeColor
			};
		}
		else
		{
			Logger.LogError($"Failed to spawn an object from {name}! Is GUI_{name} missing reference to object prefab?");
		}
	}

	IEnumerator SetMachineOperating()
	{
		MachineOperating = true;
		UpdateSprite();
		SoundManager.PlayNetworkedAtPos("PosterCreate", objectBehaviour.AssumedWorldPositionServer(), sourceObj: gameObject);
		yield return WaitFor.Seconds(DISPENSING_TIME);
		MachineOperating = false;
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
