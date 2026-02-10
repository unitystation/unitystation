using UnityEngine;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.Core.Lifecycle;
using US13.HealthV2.Living;
using US13.Items.Implants.Organs;
using US13.UI.Core.ProgressBar;
using US13.UI.Systems.Lobby;
using US13.UI.Systems.MainHUD.UI_Bottom;
using Util;

namespace US13.Items.Others.Magical.SpellBooks.potions
{
	public class PotionGenderChanging : MonoBehaviour, ICheckedInteractable<HandApply>
	{
		private static readonly StandardProgressActionConfig ProgressConfig =
			new StandardProgressActionConfig(StandardProgressActionType.SelfHeal);

		public virtual bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			if (Validations.HasComponent<LivingHealthMasterBase>(interaction.TargetObject) == false) return false;
			if (interaction.TargetObject.GetComponent<LivingHealthMasterBase>().IsDead) return false;
			return interaction.Intent == Intent.Help;
		}

		public virtual void ServerPerformInteraction(HandApply interaction)
		{
			if (CheckTarget(interaction.TargetObject))
			{
				void ProgressComplete()
				{
					ServerApplyPotion(interaction.TargetObject);
				}

				StandardProgressAction.Create(ProgressConfig, ProgressComplete)
					.ServerStartProgress(interaction.Performer.RegisterTile(), 5f,
						interaction.Performer);
			}
		}


		public void ServerApplyPotion(GameObject Target)
		{
			if (CheckTarget(Target))
			{
				var core = Target.GetComponent<LivingHealthMasterBase>();
				BodyType BodyType = BodyType.NonBinary;

				switch (core.playerSprites.ThisCharacter.BodyType)
				{
					case  BodyType.Male: //MTF
						BodyType = BodyType.Female;
						break;
					case  BodyType.Female: //FTM
						BodyType = BodyType.Male;
						break;
					case BodyType.NonBinary: //It's like complex numbers it just goes Negative complex numbers
						BodyType = BodyType.NonBinary;
						break;
					case BodyType.Other1:
						BodyType = BodyType.Other2;
						break;
					case BodyType.Other2:
						BodyType = BodyType.Other1;
						break;
				}

				var Voice = core.playerSprites.ThisCharacter.Voice;
				if (Voice.Contains("Male"))
				{
					Voice = Voice.Replace("Male", "Female");
				}
				else
				{
					Voice = Voice.Replace("Female" , "Male");
				}

				core.playerSprites.ThisCharacter.Voice = Voice;

				foreach (var BodyPart in core.BodyPartList)
				{
					if (SweetExtensions.TryGetComponentCustom<Tongue>((Component)BodyPart, out var Tongue))
					{
						Voice = Tongue.Voice;
						if (Voice.Contains("Male"))
						{
							Voice = Voice.Replace("Male", "Female");
						}
						else
						{
							Voice = Voice.Replace("Female" , "Male");
						}

						Tongue.Voice = Voice;
					}
				}

				core.playerSprites.ThisCharacter.BodyType = BodyType;
				core.playerSprites.SetAllBodyType(BodyType);

				_ = Despawn.ServerSingle(this.gameObject);
			}
		}


		public bool CheckTarget(GameObject Target)
		{
			if (Validations.HasComponent<LivingHealthMasterBase>(Target) == false) return false;
			if (Target.GetComponent<LivingHealthMasterBase>().IsDead) return false;
			return true;
		}
	}
}