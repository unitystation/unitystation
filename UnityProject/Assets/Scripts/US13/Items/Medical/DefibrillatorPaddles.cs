using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using US13.Core.Addressables.Types;
using US13.Core.Chat;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.Core.Physics;
using US13.Health.Objects;
using US13.HealthV2;
using US13.HealthV2.Living;
using US13.Items.Traits;
using US13.Managers;
using US13.Messages.Server.SoundMessages;
using US13.Mobs.Equipment;
using US13.Player.MovementV2;
using US13.Systems.Explosions;
using US13.Systems.Inventory;
using US13.UI.Core.ProgressBar;
using US13.UI.Systems.MainHUD.UI_Bottom;
using US13.UI.Systems.Tooltips.HoverTooltips;
using Util;
using Util.Independent.FluentRichText;

namespace US13.Items.Medical
{
	public class DefibrillatorPaddles : MonoBehaviour, ICheckedInteractable<HandApply>, IInteractable<HandActivate>, IHoverTooltip, IExaminable
	{
		public ItemTrait DefibrillatorTrait;

		public float Time;

		public bool DoesntRequireBackpack = false;

		[SerializeField] private AddressableAudioSource soundCharged;
		[SerializeField] private AddressableAudioSource soundReady;
		[SerializeField] private AddressableAudioSource soundSuccsuess;
		[SerializeField] private AddressableAudioSource soundFailed;
		[SerializeField] private AddressableAudioSource soundZap;

		private bool isReady;
		private bool onCooldown;
		private readonly float cooldown = 5;

		private const float PUSHBACK_FORCE = 5550f;
		private const float SPIN_FACTOR = 10f;
		private const float INAIR_TIME = 0.1f;
		private const float DAMAGE_AMOUNT = 25f;

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			if (interaction.Intent == Intent.Harm) return true;
			if (interaction.TargetObject.TryGetComponent<LivingHealthMasterBase>(out var livingHealthMaster) is false) return false;
			if (side == NetworkSide.Server && DoesntRequireBackpack == false)
			{
				var equipment = interaction.Performer.GetComponent<Equipment>();
				var ObjectInSlot = equipment.GetClothingItem(NamedSlot.back).ServerGameObjectReference;
				if (Validations.HasItemTrait(ObjectInSlot, DefibrillatorTrait) == false)
				{
					ObjectInSlot = equipment.GetClothingItem(NamedSlot.belt).ServerGameObjectReference;
					if (Validations.HasItemTrait(ObjectInSlot, DefibrillatorTrait) == false)
					{
						Chat.AddExamineMsg(interaction.Performer, "You need to place the defibrillator unit on your back or belt to use the paddles!".Color(Color.yellow));
						return false;
					}
				}
			}

			if (CanDefibrillate(livingHealthMaster, interaction.Performer) == false && side == NetworkSide.Server)
			{
				return false;
			}

			return true;
		}

		private bool CanDefibrillate(LivingHealthMasterBase livingHealthMaster, GameObject performer)
		{
			if (livingHealthMaster.brain == null || (livingHealthMaster.brain.RelatedPart.MaxHealth -
			                                         livingHealthMaster.brain.RelatedPart
				                                         .TotalDamageWithoutOxyCloneRadStam) < -100)
			{
				Chat.AddExamineMsgFromServer(performer,
					"It appears they're missing their brain or Their brain is too damaged");
				return false;
			}

			return true;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (interaction.Intent == Intent.Harm)
			{
				HarmInteraction(interaction);
				return;
			}
			void Perform()
			{
				var livingHealthMaster = interaction.TargetObject.GetComponent<LivingHealthMasterBase>();
				var objectPos = gameObject.AssumedWorldPosServer();
				if (CanDefibrillate(livingHealthMaster, interaction.Performer) == false)
				{
					_ = SoundManager.PlayNetworkedAtPosAsync(soundFailed, objectPos, new AudioSourceParameters(spatialBlend:2));
					StartCoroutine(Cooldown());
					return;
				}

				livingHealthMaster.RestartHeart();
				_ = SoundManager.PlayNetworkedAtPosAsync(soundZap, objectPos, new AudioSourceParameters(spatialBlend:2));
				if (livingHealthMaster.IsDead == false)
				{
					livingHealthMaster.playerScript.Mind.OrNull()?.StopGhosting();
					_ = SoundManager.PlayNetworkedAtPosAsync(soundSuccsuess, objectPos,  new AudioSourceParameters(spatialBlend:2));
					StartCoroutine(Cooldown());
					return;
				}

				_ = SoundManager.PlayNetworkedAtPosAsync(soundFailed, objectPos, new AudioSourceParameters(spatialBlend:2));
				StartCoroutine(Cooldown());
			}

			if (isReady == false || onCooldown == true)
			{
				Chat.AddExamineMsg(interaction.Performer,
					$"You need to charge the {gameObject.ExpensiveName()} first!");
				return;
			}

			var bar = StandardProgressAction.Create(
				new StandardProgressActionConfig(StandardProgressActionType.CPR, false, false, true), Perform);
			bar.ServerStartProgress(interaction.Performer.RegisterTile(), Time, interaction.Performer);
		}

		private IEnumerator Cooldown()
		{
			onCooldown = true;
			yield return WaitFor.Seconds(cooldown);
			onCooldown = false;
			SoundManager.PlayNetworkedAtPos(soundCharged, gameObject.AssumedWorldPosServer(), new AudioSourceParameters(spatialBlend:2));
		}

		public void ServerPerformInteraction(HandActivate interaction)
		{
			if (onCooldown)
			{
				Chat.AddExamineMsg(interaction.Performer, $"The {gameObject.ExpensiveName()} is still charging!");
				return;
			}

			if (isReady == false)
			{
				Chat.AddExamineMsg(interaction.Performer, $"You prepare the {gameObject.ExpensiveName()}");
				isReady = true;
				_ = SoundManager.PlayNetworkedAtPosAsync(soundReady, gameObject.AssumedWorldPosServer(), new AudioSourceParameters(spatialBlend:2));
				return;
			}

			Chat.AddExamineMsg(interaction.Performer,
				$"<color=green>The {gameObject.ExpensiveName()} is charged and ready to be used.</color>");
		}

		private void HarmInteraction(HandApply interaction)
		{
			if (interaction.Intent != Intent.Harm) return;
			if (onCooldown)
			{
				Chat.AddExamineMsg(interaction.Performer, $"The {gameObject.ExpensiveName()} is still charging!".Color(RichTextColor.Yellow));
				return;
			}
			if (isReady is false)
			{
				Chat.AddExamineMsg(interaction.Performer, $"You need to prepare the {gameObject.ExpensiveName()} first!".Color(RichTextColor.Yellow));
				return;
			}
			if (interaction.TargetObject.TryGetComponent<LivingHealthMasterBase>(out var livingHealthMaster) == false) return;
			if (interaction.Performer.TryGetComponent<UniversalObjectPhysics>(out var interactor) == false) return;
			var targetPos = livingHealthMaster.playerScript.AssumedWorldPos;
			var interactorPos = interaction.PerformerPlayerScript.AssumedWorldPos;
			var pushVector = (interactorPos - targetPos).To2();

			SparkUtil.TrySpark(interaction.Performer);
			interactor.NewtonianNewtonPush( pushVector, PUSHBACK_FORCE, inAirTime: INAIR_TIME, spinFactor: SPIN_FACTOR );

			//push perpetrator and victim away from each other
			livingHealthMaster.ApplyDamageToBodyPart(interaction.PerformerPlayerScript.gameObject, DAMAGE_AMOUNT,
				AttackType.Energy, DamageType.Burn, interaction.TargetBodyPart);
			livingHealthMaster.playerScript.playerMove.NewtonianNewtonPush(-pushVector, PUSHBACK_FORCE, inAirTime: INAIR_TIME, spinFactor: SPIN_FACTOR);

			HandlePullingActorsDuringHarmInteraction(livingHealthMaster, pushVector, interactor);
			StartCoroutine(Cooldown());
			Chat.AddAttackMsgToChat(interactor.gameObject, livingHealthMaster.gameObject, interaction.TargetBodyPart, gameObject, "shocked");
		}

		private void HandlePullingActorsDuringHarmInteraction(LivingHealthMasterBase victim, Vector2 pushVector, UniversalObjectPhysics perp)
		{
			if (victim.playerScript.playerMove.PulledBy.HasComponent == false) return;
			victim.playerScript.playerMove.PulledBy.Component.NewtonianNewtonPush(-pushVector, PUSHBACK_FORCE, inAirTime: INAIR_TIME, spinFactor: SPIN_FACTOR);
			if (victim.playerScript.playerMove.PulledBy.Component is MovementSynchronisation sync && sync == perp)
			{
				sync.playerScript.playerHealth.ApplyDamageToBodyPart(sync.gameObject, DAMAGE_AMOUNT,
					AttackType.Energy, DamageType.Burn);
			}
		}

		public string HoverTip()
		{
			return Examine();
		}

		public string CustomTitle()
		{
			return null;
		}

		public Sprite CustomIcon()
		{
			return null;
		}

		public List<Sprite> IconIndicators()
		{
			return null;
		}

		public List<TextColor> InteractionsStrings()
		{
			return new List<TextColor>()
			{
				new TextColor()
				{
					Text = "Hand-Activate to prepare the paddles.".Color(Color.green),
					Color = Color.green
				},
				new TextColor()
				{
					Text = "Apply on a person to attempt to revive them while charged.".Color(Color.green),
					Color = Color.green
				},
				new TextColor()
				{
					Text = "Attack while charged to shock your victim.".Color(Color.red),
					Color = Color.red
				}
			};
		}

		public string Examine(Vector3 worldPos = default)
		{
			var SB = new StringBuilder();
			if (DoesntRequireBackpack)
			{
				SB.AppendLine("It doesn't require its unit to be placed on your back or belt to be used.".Color(Color.yellow));
			}
			else
			{
				SB.AppendLine("It requires the unit to be equipped on your back or belt to be used.".Color(Color.yellow));
			}

			return SB.ToString();
		}
	}
}