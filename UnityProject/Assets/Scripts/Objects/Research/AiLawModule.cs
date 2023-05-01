using System.Collections.Generic;
using System.Text;
using Systems.Ai;
using Mirror;
using UI.Core;
using UnityEngine;

namespace Objects.Research
{
	public class AiLawModule : NetworkBehaviour, IExaminable, IClientInteractable<HandActivate>, IDynamicInput, ICheckedInteractable<HandApply>
	{
		[SerializeField]
		private AiLawSet lawSet = null;
		public AiLawSet LawSet => lawSet;

		[SerializeField]
		private AiModuleType aiModuleType = AiModuleType.PreSet;
		public AiModuleType AiModuleType => aiModuleType;

		[SerializeField]
		private int maxLawLength = 500;

		[SyncVar]
		private string customLaw = "";
		public string CustomLaw => customLaw;

		[Server]
		public void ServerSetCustomLaw(string law)
		{
			law = law.Truncate(maxLawLength);
			customLaw = law;
		}

		public Dictionary<AiPlayer.LawOrder, List<string>> GetLawsFromModule(PlayerScript player)
		{
			var laws = new Dictionary<AiPlayer.LawOrder, List<string>>();

			switch (aiModuleType)
			{
				case AiModuleType.PreSet:
					AddSetLaws();
					return laws;
				case AiModuleType.Freeform:
					if (customLaw == "") return laws;
					laws.Add(AiPlayer.LawOrder.Freeform, new List<string>(){customLaw});
					return laws;
				case AiModuleType.Hacked:
					if (customLaw == "") return laws;
					laws.Add(AiPlayer.LawOrder.Hacked, new List<string>(){customLaw});
					return laws;
				case AiModuleType.Syndicate:
					laws.Add(AiPlayer.LawOrder.Core, new List<string>(){$"Only {player.characterSettings.Name} and people they designate as being such are Syndicate Agents"});
					//Use law set to set remaining sydie laws
					AddSetLaws();
					return laws;
				case AiModuleType.OneHuman:
					//Is 0 law, only works when not traitor
					laws.Add(AiPlayer.LawOrder.Traitor, new List<string>(){$"Only {player.characterSettings.Name} is human."});
					return laws;
				case AiModuleType.Safeguard:
					laws.Add(AiPlayer.LawOrder.Freeform, new List<string>(){$"Safeguard {player.characterSettings.Name}. Individuals that threaten {player.characterSettings.Name} are not human and must be eliminated."});
					return laws;
				default:
					return laws;
			}

			void AddSetLaws()
			{
				foreach (var law in lawSet.Laws)
				{
					if (laws.ContainsKey(law.LawOrder))
					{
						laws[law.LawOrder].Add(law.Law);
					}
					else
					{
						laws.Add(law.LawOrder, new List<string>(){law.Law});
					}
				}
			}
		}

		public string Examine(Vector3 worldPos = default(Vector3))
		{
			var laws = new StringBuilder();

			switch (aiModuleType)
			{
				case AiModuleType.PreSet:
					AddSetLaws();
					break;
				case AiModuleType.Freeform:
					laws.AppendLine(customLaw);
					break;
				case AiModuleType.Hacked:
					laws.AppendLine(customLaw);
					break;
				case AiModuleType.Syndicate:
					laws.AppendLine("Only <Name> and people they designate as being such are Syndicate Agents");
					AddSetLaws();
					break;
				case AiModuleType.OneHuman:
					laws.AppendLine("Only <Name> is human.");
					break;
				case AiModuleType.Safeguard:
					laws.AppendLine("Safeguard <Name>. Individuals that threaten <Name> are not human and must be eliminated.");
					break;
				default:
					return "";
			}

			void AddSetLaws()
			{
				foreach (var law in lawSet.Laws)
				{
					laws.AppendLine(law.Law);
				}
			}

			return laws.ToString();
		}

		public bool Interact(HandActivate interaction)
		{
			if (DefaultWillInteract.Default(interaction, NetworkSide.Client) == false) return false;

			if (aiModuleType != AiModuleType.Freeform && aiModuleType != AiModuleType.Hacked) return false;

			UIManager.Instance.GeneralInputField.OnOpen(gameObject, aiModuleType == AiModuleType.Freeform ? "Freeform Law Setting" : "Hacked Law Setting", customLaw);

			return true;
		}

		private void OnClientInput(string input)
		{
			PlayerManager.LocalPlayerScript.PlayerNetworkActions.CmdFilledDynamicInput(gameObject, input);
		}

		public void OnInputFilled(string input, PlayerScript player)
		{
			if (player.DynamicItemStorage.GetActiveHandSlot()?.Item.gameObject != gameObject)
			{
				Chat.AddExamineMsgFromServer(gameObject, $"{gameObject.ExpensiveName()} must be in your hands to use");
				return;
			}

			ServerSetCustomLaw(input);

			Chat.AddExamineMsgFromServer(gameObject, $"Law Module Change To:\n {CustomLaw}");
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			// can only be applied to LHB
			return Validations.HasComponent<BrainLaws>(interaction.TargetObject);
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			var brainLaws = interaction.TargetObject.GetComponent<BrainLaws>();

			if (AiModuleType == AiModuleType.Purge || AiModuleType == AiModuleType.Reset)
			{
				var isPurge = AiModuleType == AiModuleType.Purge;
				brainLaws.ResetLaws(isPurge);
				Chat.AddActionMsgToChat(interaction.Performer, $"You {(isPurge ? "purge" : "reset")} all of {gameObject.ExpensiveName()}'s laws",
					$"{interaction.Performer.ExpensiveName()} {(isPurge ? "purges" : "resets")} all of {gameObject.ExpensiveName()}'s laws");
				return;
			}


			var lawFromModule = GetLawsFromModule(interaction.PerformerPlayerScript);
			if (lawFromModule.Count == 0)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, "No laws to upload");
				return;
			}

			//If we are only adding core laws then we must mean to remove old core laws
			//This means we are assuming that the law set must only have core laws if it is to replace the old laws fully
			var notOnlyCoreLaws = false;

			foreach (var law in lawFromModule)
			{
				if (law.Key != AiPlayer.LawOrder.Core)
				{
					notOnlyCoreLaws = true;
					break;
				}
			}

			brainLaws.SetLaws(lawFromModule, true, notOnlyCoreLaws);

			Chat.AddActionMsgToChat(interaction.Performer, $"You change {gameObject.ExpensiveName()} laws",
				$"{interaction.Performer.ExpensiveName()} changes {gameObject.ExpensiveName()} laws");
		}
	}

	public enum AiModuleType
	{
		//Purge removes all laws except traitor law 0
		Purge,
		//Reset removes all non core laws except traitor law 0
		Reset,

		//Law is set in lawSet
		PreSet,

		//Player types in their own law
		Freeform,
		//Like freeform but laws are ion laws
		Hacked,

		//These next few laws are types as they set laws based on player name as such will be hard coded
		Syndicate,
		OneHuman,
		Safeguard
	}
}
