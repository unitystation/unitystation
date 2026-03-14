using System.Collections;
using System.Linq;
using UnityEngine;
using US13.Core.Chat;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.Core.Sprite_Handler;
using US13.HealthV2;
using US13.HealthV2.Living;
using US13.Player;
using US13.Tilemaps.Behaviours.Objects;
using US13.UI.Core.ProgressBar;
using US13.UI.Systems.MainHUD.UI_Bottom;
using Util;

namespace US13.Objects.ExecutionDevices
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

		public float ExecuteTime = 6;

		private static readonly StandardProgressActionConfig injectProgressBar =
			new StandardProgressActionConfig(StandardProgressActionType.Restrain);

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
				Chat.AddWarningMsgFromServer(interaction.Performer, "Can't do anything while the iron is not raised!");
				return;
			}
			if (victimStorage.GetStoredObjects().Count() != 0)
			{
				Chat.AddWarningMsgFromServer(interaction.Performer, "There's already something in this device!");
				return;
			}

			if(interaction.DroppedObject.TryGetComponent<PlayerScript>(out var player) == false) return;
			victimStorage.StoreObject(interaction.DroppedObject);
			Chat.AddActionMsgToChat(interaction.Performer, $"You load {interaction.DroppedObject.ExpensiveName()} into the guillotine!",
				$"{interaction.Performer.ExpensiveName()} load {interaction.DroppedObject.ExpensiveName()} into the guillotine!");

			OnEnterDevice(interaction.DroppedObject, interaction.Performer);
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			return DefaultWillInteract.Default(interaction, side);
		}

		private bool IsBusy(GameObject performer)
		{
			if (isBusy == false) return false;
			if (performer != null) Chat.AddWarningMsgFromServer(performer, "Can't do that right now..");
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
				if (interaction.Intent == Intent.Help)
				{
					//here
					((IExecutionDevice) this).Controller.ReleaseVictim();
				}
				else if (interaction.Intent == Intent.Harm)
				{
					//here
					StandardProgressAction.Create(injectProgressBar,
							() => ((IExecutionDevice)this).Controller.Execute())
						.ServerStartProgress(interaction.Performer.GetComponent<RegisterTile>(), ExecuteTime, interaction.Performer.gameObject);
				}


			}
		}
	}
}