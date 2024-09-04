using System.Collections;
using UnityEngine;
using Mirror;
using AddressableReferences;
using Core;
using Items.Atmospherics;
using Logs;
using Objects.Construction;


namespace Objects.Atmospherics
{
	public class PipeDispenser : MonoBehaviour, ICheckedInteractable<HandApply>
	{
		private const float DISPENSING_TIME = 2; // As per sprite sheet JSON file.

		[SerializeField]
		private AddressableAudioSource OperatingSound = null;

		[SerializeField]
		private ItemTrait pipeDispensedItemTrait = null;

		private UniversalObjectPhysics objectPhysics;
		private WrenchSecurable securable;
		private HasNetworkTab netTab;
		private SpriteHandler spriteHandler;

		private Coroutine animationRoutine;

		public bool MachineOperating { get; private set; } = false;

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
			objectPhysics = GetComponent<UniversalObjectPhysics>();
			securable = GetComponent<WrenchSecurable>();
			netTab = GetComponent<HasNetworkTab>();
			spriteHandler = transform.GetChild(0).GetComponent<SpriteHandler>();

			securable.OnAnchoredChange.AddListener(OnAnchoredChange);
		}

		private void UpdateSprite()
		{
			if (MachineOperating)
			{
				spriteHandler.SetCatalogueIndexSprite((int)SpriteState.Operating);
			}
			else
			{
				spriteHandler.SetCatalogueIndexSprite((int)SpriteState.Idle);
			}
		}

		public void Dispense(GameObject objectPrefab, PipeLayer pipeLayer, Color pipeColor)
		{
			if (MachineOperating || securable.IsAnchored == false) return;

			this.RestartCoroutine(SetMachineOperating(), ref animationRoutine);
			SpawnResult spawnResult = Spawn.ServerPrefab(objectPrefab, objectPhysics.registerTile.WorldPosition);
			if (spawnResult.Successful == false)
			{
				Loggy.LogError(
						$"Failed to spawn an object from {name}! " +
						$"Is {nameof(UI.Objects.Atmospherics.GUI_PipeDispenser)} missing reference to object prefab?",
						Category.Pipes);
				return;
			}

			foreach (var spriteHandler in spawnResult.GameObject.GetComponentsInChildren<SpriteHandler>())
			{
				spriteHandler.SetColor(pipeColor);
			}

			if (spawnResult.GameObject.TryGetComponent<PipeItem>(out var pipe))
			{
				pipe.Colour = pipeColor;
			}
		}

		private IEnumerator SetMachineOperating()
		{
			MachineOperating = true;
			UpdateSprite();
			SoundManager.PlayNetworkedAtPos(OperatingSound, objectPhysics.registerTile.WorldPosition, sourceObj: gameObject);
			yield return WaitFor.Seconds(DISPENSING_TIME);
			MachineOperating = false;
			UpdateSprite();
		}

		private void OnAnchoredChange()
		{
			netTab.enabled = securable.IsAnchored;
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			return interaction.UsedObject != null && Validations.HasItemTrait(interaction.UsedObject, pipeDispensedItemTrait);
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (Validations.HasItemTrait(interaction.UsedObject, pipeDispensedItemTrait))
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, $"You put the {interaction.UsedObject.ExpensiveName()} back into the {gameObject.ExpensiveName()}");
				_= Despawn.ServerSingle(interaction.UsedObject);
			}
		}
	}
}
