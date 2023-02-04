using System.Collections;
using System.Linq;
using HealthV2;
using UnityEngine;

namespace Objects.ExecutionDevices
{
	[RequireComponent(typeof(ExecutionDeviceController))]
	public class Guillotine : MonoBehaviour, IExecutionDevice, ICheckedInteractable<MouseDrop>, ICheckedInteractable<HandApply>
	{
		[SerializeField] private ExecutionDeviceController controller;
		[SerializeField] private SpriteHandler spriteHandler;
		[SerializeField] private SpriteHandler headSpriteHandler;
		[SerializeField] private SpriteHandler bloodSpriteOverlayHandler;
		[SerializeField] private SpriteDataSO headSpriteTemp;
		[SerializeField] private ObjectContainer victimStorage;

		private bool isRaised = true;
		private bool isBusy = false;
		private int bloodyLevel = 0;

		private const int ANIM_DROP = 1;
		private const int ANIM_RAISE = 3;

		ExecutionDeviceController IExecutionDevice.Controller
		{
			get => controller == null ? GetComponent<ExecutionDeviceController>() : controller;
			set => controller = value;
		}

		private void Awake()
		{
			controller = GetComponent<ExecutionDeviceController>();
			if (victimStorage == null) victimStorage = GetComponent<ObjectContainer>();
		}

		public void OnEnterDevice(GameObject target, GameObject executioner = null)
		{
			((IExecutionDevice)this).Controller.Victim = target;
			headSpriteHandler.SetSpriteSO(headSpriteTemp);
		}

		public void OnLeaveDevice(GameObject target, GameObject executioner = null)
		{
			((IExecutionDevice)this).Controller.Victim = null;
			victimStorage.RetrieveObjects();
			headSpriteHandler.Empty();
			RaiseIron();
		}

		public IEnumerator ExecuteTarget()
		{
			if (IsBusy(null)) yield break;
			if (isRaised == false)
			{
				yield break;
			}
			if (controller.Victim.TryGetComponent<LivingHealthMasterBase>(out var health) == false) yield break;
			isRaised = false;
			isBusy = true;
			spriteHandler.AnimateOnce(ANIM_DROP);
			health.IndicatePain(15000, true);
			foreach (var bodyPart in health.SurfaceBodyParts)
			{
				if (bodyPart.BodyPartType != BodyPartType.Head) continue;
				bodyPart.TryRemoveFromBody();
				RaiseBloodyLevel();
				break;
			}
			health.Death();
			yield return WaitFor.Seconds(1.25f);
			OnLeaveDevice(controller.Victim);
		}

		private void RaiseIron()
		{
			spriteHandler.AnimateOnce(ANIM_RAISE);
			isRaised = true;
			isBusy = false;
		}

		private void RaiseBloodyLevel()
		{
			if (bloodyLevel >= 3) return;
			bloodyLevel++;
			bloodSpriteOverlayHandler.AnimateOnce(bloodyLevel - 1);
		}

		public bool WillInteract(MouseDrop interaction, NetworkSide side)
		{
			return DefaultWillInteract.Default(interaction, side);
		}

		public void ServerPerformInteraction(MouseDrop interaction)
		{
			if (IsBusy(interaction.Performer)) return;
			if (isRaised == false)
			{
				Chat.AddExamineMsg(interaction.Performer, "Can't do anything while the iron is not raised!");
				return;
			}
			if (victimStorage.GetStoredObjects().Count() != 0)
			{
				Chat.AddExamineMsg(interaction.Performer, "There's already something in this device!");
				return;
			}
			if (interaction.DroppedObject.Player() == null) return;
			victimStorage.StoreObject(interaction.DroppedObject);
			OnEnterDevice(interaction.DroppedObject, interaction.Performer);
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			return DefaultWillInteract.Default(interaction, side);
		}

		private bool IsBusy(GameObject performer)
		{
			if (isBusy == false) return false;
			if (performer != null) Chat.AddExamineMsg(performer, "Can't do that right now..");
			return true;

		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (IsBusy(interaction.Performer)) return;
			if (isRaised == false)
			{
				RaiseIron();
				return;
			}
			else
			{
				((IExecutionDevice)this).Controller.Execute();
			}
		}
	}
}