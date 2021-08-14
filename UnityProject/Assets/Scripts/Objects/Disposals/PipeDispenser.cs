using System.Collections;
using UnityEngine;
using Mirror;
using AddressableReferences;
using Items.Atmospherics;
using Objects.Construction;


namespace Objects.Atmospherics
{
	public class PipeDispenser : NetworkBehaviour
	{
		private const float DISPENSING_TIME = 2; // As per sprite sheet JSON file.

		[SerializeField]
		private AddressableAudioSource OperatingSound = null;

		private ObjectBehaviour objectBehaviour;
		private WrenchSecurable securable;
		private HasNetworkTab netTab;
		private SpriteHandler spriteHandler;

		private Coroutine animationRoutine;

		public bool MachineOperating { get; private set; } = false;

		[SyncVar(hook = nameof(SyncObjectProperties))]
		private PipeObjectSettings newPipe;

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

		private void Awake()
		{
			objectBehaviour = GetComponent<ObjectBehaviour>();
			securable = GetComponent<WrenchSecurable>();
			netTab = GetComponent<HasNetworkTab>();
			spriteHandler = transform.GetChild(0).GetComponent<SpriteHandler>();

			securable.OnAnchoredChange.AddListener(OnAnchoredChange);
		}

		private void UpdateSprite()
		{
			if (MachineOperating)
			{
				spriteHandler.ChangeSprite((int)SpriteState.Operating);
			}
			else
			{
				spriteHandler.ChangeSprite((int)SpriteState.Idle);
			}
		}

		private void SyncObjectProperties(PipeObjectSettings oldState, PipeObjectSettings newState)
		{
			newPipe = newState;
			newPipe.pipeObject.GetComponentInChildren<SpriteRenderer>().color = newPipe.pipeColor;
		}

		public void Dispense(GameObject objectPrefab, PipeLayer pipeLayer, Color pipeColor)
		{
			if (MachineOperating || securable.IsAnchored == false) return;

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
				Logger.LogError($"Failed to spawn an object from {name}! Is GUI_{name} missing reference to object prefab?",
					Category.Pipes);
			}
		}

		private IEnumerator SetMachineOperating()
		{
			MachineOperating = true;
			UpdateSprite();
			SoundManager.PlayNetworkedAtPos(OperatingSound, objectBehaviour.AssumedWorldPositionServer(), sourceObj: gameObject);
			yield return WaitFor.Seconds(DISPENSING_TIME);
			MachineOperating = false;
			UpdateSprite();
		}

		private void OnAnchoredChange()
		{
			netTab.enabled = securable.IsAnchored;
		}

		private struct PipeObjectSettings
		{
			public GameObject pipeObject;
			public Color pipeColor;
		}
	}
}
