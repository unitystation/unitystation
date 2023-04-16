using System;
using System.Linq;
using HealthV2;
using Managers;
using Player.Language;
using UnityEngine;

namespace Items.Bureaucracy
{
	public class LanguageBook : MonoBehaviour, ICheckedInteractable<HandApply>, ICheckedInteractable<HandActivate>, IExaminable
	{
		[SerializeField]
		private LanguageSO languageToLearn = null;

		[SerializeField]
		private RandomItemPool randomBook = null;

		[SerializeField]
		[Tooltip("-1 means infinite")]
		private int maxCharges = -1;

		[SerializeField]
		[TextArea(5,5)]
		private string flavourText = "suddenly your mind is filled with codewords and responses";

		private int usedCharges = 0;

		private UniversalObjectPhysics objectPhysics;

		//Used by admins to try to set this books language
		private string languageToSet;

		private void Awake()
		{
			objectPhysics = GetComponent<UniversalObjectPhysics>();
			this.GetComponent<SimpleBook>().OnBookRead += (PlayerInfo playerInfo) =>
			{
				SelfTeach(playerInfo.Script);
			};
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (interaction.TargetObject == null) return false;

			if (interaction.TargetObject.GetComponent<MobLanguages>() == null) return false;

			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			if (interaction.IsHighlight == false && side == NetworkSide.Server && maxCharges != -1)
			{
				if (usedCharges >= maxCharges)
				{
					Chat.AddExamineMsgFromServer(interaction.Performer, "No charges left!");
					return false;
				}
			}

			return true;
		}

		public virtual void ServerPerformInteraction(HandApply interaction)
		{
			if (interaction.TargetObject == interaction.Performer)
			{
				SelfTeach(interaction.PerformerPlayerScript);
				return;
			}

			if (interaction.TargetObject.TryGetComponent<LivingHealthMasterBase>(out var health))
			{
				if (health.IsDead)
				{
					Chat.AddActionMsgToChat(interaction.Performer,
						$"You smack {health.playerScript.visibleName}'s lifeless corpse with {gameObject.ExpensiveName()}.",
						$"{interaction.PerformerPlayerScript.visibleName} smacks {health.playerScript.visibleName}'s lifeless corpse with {gameObject.ExpensiveName()}.");
					return;
				}
			}

			if(interaction.TargetObject.TryGetComponent<MobLanguages>(out var mobLanguages) == false) return;

			if (mobLanguages.CanSpeakLanguage(languageToLearn))
			{
				Chat.AddActionMsgToChat(interaction.Performer,
					$"You beat {health.playerScript.visibleName} over the head with {gameObject.ExpensiveName()}.",
					$"{interaction.PerformerPlayerScript.visibleName} beats {health.playerScript.visibleName} over the head with {gameObject.ExpensiveName()}.");
				return;
			}

			mobLanguages.LearnLanguage(languageToLearn, true);

			Chat.AddActionMsgToChat(interaction.Performer,
				$"You teach {health.playerScript.visibleName} by beating {health.playerScript.characterSettings.ThemPronoun(health.playerScript)} over the head with {gameObject.ExpensiveName()}.",
				$"{interaction.PerformerPlayerScript.visibleName} teaches {health.playerScript.visibleName} by beating {health.playerScript.characterSettings.ThemPronoun(health.playerScript)} over the head with {gameObject.ExpensiveName()}.");

			UseCharge(interaction.PerformerPlayerScript);
		}

		public bool WillInteract(HandActivate interaction, NetworkSide side)
		{
			if (interaction.IsAltClick == false) return false;

			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			return true;
		}

		public virtual void ServerPerformInteraction(HandActivate interaction)
		{
			SelfTeach(interaction.PerformerPlayerScript);
		}

		private void SelfTeach(PlayerScript performer)
		{
			if (performer.playerHealth.IsDead)
			{
				Chat.AddExamineMsgFromServer(performer.gameObject, $"You are dead, you cannot read {gameObject.ExpensiveName()}");
				return;
			}

			var mobLanguages = performer.MobLanguages;

			if (mobLanguages.CanSpeakLanguage(languageToLearn))
			{
				Chat.AddExamineMsgFromServer(performer.gameObject, $"You start skimming through {gameObject.ExpensiveName()}, but you already know {languageToLearn.LanguageName}.");
				return;
			}

			mobLanguages.LearnLanguage(languageToLearn, true);

			Chat.AddExamineMsgFromServer(performer.gameObject, $"You start skimming through {gameObject.ExpensiveName()}, and {flavourText}.");

			UseCharge(performer);
		}

		private void UseCharge(PlayerScript performer)
		{
			usedCharges++;

			if(maxCharges == -1) return;

			Chat.AddExamineMsgFromServer(performer.gameObject, $"The cover and contents of {gameObject.ExpensiveName()} start shifting and changing! It slips out of your hands!");

			//Spawn random book
			_ = Spawn.ServerPrefab(randomBook.Pool.GetRandom().Prefab, objectPhysics.OfficialPosition);

			//Despawn this book
			_ = Despawn.ServerSingle(gameObject);
		}

		//Used by admin VV to set book from language string
		private void SetFromString()
		{
			var languageFound = LanguageManager.Instance.AllLanguages.FirstOrDefault(x =>
				x.LanguageName == languageToSet || x.name == languageToSet);

			if(languageFound == null) return;

			languageToLearn = languageFound;
			Chat.AddActionMsgToChat(gameObject, $"The book transforms into the book for {languageToLearn.LanguageName}.");

			var itemAtt = GetComponent<ItemAttributesV2>();
			itemAtt.ServerSetArticleName($"{languageToLearn.LanguageName} Manual");
			itemAtt.ServerSetArticleDescription($"A manual to learn {languageToLearn.LanguageName}");
		}

		public string Examine(Vector3 worldPos = default(Vector3))
		{
			if (maxCharges == -1)
			{
				return "This is the extended edition";
			}

			var chargesLeft = maxCharges - usedCharges;
			return usedCharges < maxCharges ? $"It has {chargesLeft} charge{(chargesLeft == 1 ? "" : "s")} left!" : "No charges left!";
		}
	}
}