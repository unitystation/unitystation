using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Mirror;
using Player;

namespace HealthV2
{
	public class Dissectible : NetworkBehaviour, IClientInteractable<HandApply>,
		ICheckedInteractable<HandApply>
	{
		public LivingHealthMasterBase LivingHealthMasterBase;

		[SyncVar(hook = nameof(SetProcedureInProgress))]
		public bool ProcedureInProgress = false;

		private GameObject InternalcurrentlyOn = null;

		public GameObject currentlyOn
		{
			get
			{
				if (isServer)
				{
					return InternalcurrentlyOn;
				}

				var spawned = CustomNetworkManager.IsServer ? NetworkServer.spawned : NetworkClient.spawned;
				if (spawned.TryGetValue(BodyPartID, out var bodyPart) && bodyPart != null)
				{
					return bodyPart.gameObject;
				}

				return null;
			}
			set
			{
				SetBodyPartID(BodyPartID, value ? value.GetComponent<NetworkIdentity>().netId : NetId.Invalid);
				InternalcurrentlyOn = value;
			}
		}

		public BodyPart BodyPartIsOn => currentlyOn?.GetComponent<BodyPart>();

		[SyncVar(hook = nameof(SetBodyPartIsOpen))]
		private bool BodyPartIsopen = false;

		public bool GetBodyPartIsopen => BodyPartIsopen;

		[SyncVar(hook = nameof(SetBodyPartID))]
		private uint BodyPartID;

		public PresentProcedure ThisPresentProcedure = new PresentProcedure();

		public List<ItemTrait> InitiateSurgeryItemTraits = new List<ItemTrait>(); //Make sure to include implantable stuff

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (interaction.Intent != Intent.Help) return false; //TODO problem with surgery in Progress and Trying to use something on help content on them
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			if (ProcedureInProgress == false) return false;
			var registerPlayer = interaction.TargetObject.GetComponent<RegisterPlayer>();

			if (registerPlayer == null) return false; //Player script changes needed
			if (registerPlayer.IsLayingDown == false) return false;
			return (true);
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (ProcedureInProgress)
			{
				ThisPresentProcedure.TryTool(interaction);
			}
		}

		public bool Interact(HandApply interaction)
		{
			if (DefaultWillInteract.Default(interaction, NetworkSide.Client) == false) return false;
			//**Client**
			if (Validations.HasComponent<Dissectible>(interaction.TargetObject) == false) return false;

			var registerPlayer = interaction.TargetObject.GetComponent<RegisterPlayer>();
			if (registerPlayer == null || registerPlayer.IsLayingDown == false) return false; //Player script changes needed

			if (ProcedureInProgress == false)
			{
				if (KeyboardInputManager.Instance.CheckKeyAction(KeyAction.InteractionModifier,
					KeyboardInputManager.KeyEventType.Hold) == false)
				{
					return false;
				}


				if (Validations.HasAnyTrait(interaction.HandObject, InitiateSurgeryItemTraits) == false)
				{
					return false;
				}
			}
			else
			{
				if (Validations.HasAnyTrait(interaction.HandObject, InitiateSurgeryItemTraits) == false)
				{
					return false;
				}
			}

			if (ProcedureInProgress == false) //Defer to server
			{
				if (BodyPartIsOn != null) //Body part not picked?
				{
					if (BodyPartIsopen)
					{
						RequestBodyParts.Send(this.gameObject);
						return true;
						//Show dialogue for  Pick organ and Procedure set it
					}
					else
					{
						// var Options = currentlyOn;
						// UIManager.Instance.SurgeryDialogue.ShowDialogue(this, Options);
						RequestBodyParts.Send(this.gameObject);
						return true;
						//Show dialogue for possible surgeries
					}
				}
				else
				{
					RequestBodyParts.Send(this.gameObject, interaction.TargetBodyPart);
					return true;
					//showDialogue box, For which body part
					// Set currently on and choose what surgery
				}
			}
			else
			{
				return false; //?
				//Pass over to Body part being operated on
			}
		}

		public void SetBodyPartIsOpen(bool oldState, bool newState)
		{
			BodyPartIsopen = newState;
		}

		public void SetProcedureInProgress(bool oldState, bool newState)
		{
			ProcedureInProgress = newState;
		}

		public void SetBodyPartID(uint oldState, uint newState)
		{
			BodyPartID = newState;
		}

		public void ServerCheck(SurgeryProcedureBase surgeryProcedureBase, BodyPart onBodyPart)
		{
		    if (ProcedureInProgress) return; // Defer to server
		    // Check if a body part is currently being operated on
		    if (BodyPartIsOn != null)
		    {
		        // If the body part is open
		        if (BodyPartIsopen)
		        {
		            // Check if the surgery requires setup and perform it if needed
		            if (CheckAndSetupProcedure(onBodyPart, surgeryProcedureBase)) return;
		            // If the surgery does not require setup, check if the correct body part is being operated on
		            if (currentlyOn != onBodyPart.gameObject) return;
		            // If the correct body part is being operated on, setup the surgery procedure
		            SetupSurgeryProcedure(surgeryProcedureBase, BodyPartIsOn);
		        }
		        else
		        {
		            // If the body part is closed, check if the correct body part is being operated on
		            if (onBodyPart.gameObject != currentlyOn) return;
		            // If the correct body part is being operated on, setup the surgery procedure
		            SetupSurgeryProcedureWithoutSetup(surgeryProcedureBase, BodyPartIsOn);
		        }
		    }
		    else
		    {
		        // If no body part is currently being operated on, check available body parts
		        CheckAvailableBodyParts(surgeryProcedureBase, onBodyPart);
		    }
		}

		// Check if the surgery requires setup and perform it if needed
		private bool CheckAndSetupProcedure(BodyPart onBodyPart, SurgeryProcedureBase surgeryProcedureBase)
		{
		    foreach (var organBodyPart in BodyPartIsOn.ContainBodyParts.Where(organBodyPart => organBodyPart == onBodyPart))
		    {
		        foreach (var procedure in organBodyPart.SurgeryProcedureBase
		                     .Where(procedure => procedure is not (CloseProcedure or ImplantProcedure))
		                     .Where(procedure => surgeryProcedureBase == procedure))
		        {
		            currentlyOn = organBodyPart.gameObject;
		            ThisPresentProcedure.SetupProcedure(this, organBodyPart, procedure);
		            return true;
		        }
		        return true;
		    }
		    return false;
		}

		/// <summary>
		/// Setup the surgery procedure when the body part is open
		/// </summary>
		/// <param name="surgeryProcedureBase"></param>
		/// <param name="bodyPart"></param>
		private void SetupSurgeryProcedure(SurgeryProcedureBase surgeryProcedureBase, BodyPart bodyPart)
		{
		    foreach (var Procedure in bodyPart.SurgeryProcedureBase
		                 .Where(procedure => procedure is CloseProcedure or ImplantProcedure)
		                 .Where(procedure => surgeryProcedureBase == procedure))
		    {
		        this.currentlyOn = currentlyOn;
		        ThisPresentProcedure.SetupProcedure(this, BodyPartIsOn, Procedure);
		        return;
		    }
		}

		// Setup the surgery procedure when the body part is closed
		private void SetupSurgeryProcedureWithoutSetup(SurgeryProcedureBase surgeryProcedureBase, BodyPart bodyPart)
		{
		    foreach (var procedure in bodyPart.SurgeryProcedureBase)
		    {
		        if (procedure is CloseProcedure or ImplantProcedure) continue;
		        if (surgeryProcedureBase != procedure) continue;
		        this.currentlyOn = currentlyOn;
		        this.ThisPresentProcedure.SetupProcedure(this, BodyPartIsOn, procedure);
		        return;
		    }
		}

		// Check available body parts for surgery
		private void CheckAvailableBodyParts(SurgeryProcedureBase surgeryProcedureBase, BodyPart onBodyPart)
		{
		    foreach (var bodyPart in LivingHealthMasterBase.BodyPartList)
		    {
			    if (bodyPart != onBodyPart) continue;
			    foreach (var Procedure in bodyPart.SurgeryProcedureBase
				             .Where(procedure => procedure is not (CloseProcedure or ImplantProcedure))
				             .Where(procedure => surgeryProcedureBase == procedure))
			    {
				    this.currentlyOn = bodyPart.gameObject;
				    this.ThisPresentProcedure.SetupProcedure(this, bodyPart, Procedure);
				    return;
			    }
		    }

		    // If no matching body part is found, check for root implant procedure
		    if (LivingHealthMasterBase.GetComponent<PlayerSprites>().RaceBodyparts.Base.RootImplantProcedure !=
		        surgeryProcedureBase) return;
		    this.currentlyOn = LivingHealthMasterBase.gameObject;
		    this.ThisPresentProcedure.SetupProcedure(this, null, surgeryProcedureBase);
		}

		public void SendClientBodyParts(PlayerInfo SentByPlayer, BodyPartType inTargetBodyPart = BodyPartType.None)
		{
			if (currentlyOn == null)
			{
				var targetedBodyParts = new List<BodyPart>();
				foreach (var bodyPart in LivingHealthMasterBase.SurfaceBodyParts)
				{
					targetedBodyParts.Add(bodyPart);
				}

				SendSurgeryBodyParts.SendTo(targetedBodyParts, this, SentByPlayer);
			}
			else
			{
				if (BodyPartIsopen)
				{
					SendSurgeryBodyParts.SendTo(BodyPartIsOn.ContainBodyParts, this, SentByPlayer);
				}
				else
				{
					SendSurgeryBodyParts.SendTo(new List<BodyPart>() {BodyPartIsOn}, this, SentByPlayer);
				}
			}
		}

		public void ReceivedSurgery(List<BodyPart> Options)
		{
			if (ProcedureInProgress == false)
			{
				if (BodyPartIsOn != null)
				{
					if (BodyPartIsopen)
					{
						UIManager.Instance.SurgeryDialogue.ShowDialogue(this, Options);
					}
					else
					{
						UIManager.Instance.SurgeryDialogue.ShowDialogue(this, Options);
					}
				}
				else
				{
					UIManager.Instance.SurgeryDialogue.ShowDialogue(this, Options, true);
				}
			}
		}

		public class PresentProcedure
		{
			System.Random RNG = new System.Random();

			public Dissectible isOn;

			public SurgeryProcedureBase SurgeryProcedureBase;
			public int CurrentStep = 0;
			public BodyPart RelatedBodyPart;

			//Used for when surgeries are cancelled
			public BodyPart PreviousBodyPart;


			public HandApply Stored;
			public SurgeryStep ThisSurgeryStep;


			public void TryTool(HandApply interaction)
			{
				Stored = interaction;
				ThisSurgeryStep = null;

				ThisSurgeryStep = SurgeryProcedureBase.SurgerySteps[CurrentStep];

				if (ThisSurgeryStep != null)
				{
					if (Validations.HasItemTrait(interaction.HandObject, ThisSurgeryStep.RequiredTrait))
					{
						string StartSelf = ApplyChatModifiers(ThisSurgeryStep.StartSelf);
						string StartOther = ApplyChatModifiers(ThisSurgeryStep.StartOther);
						string SuccessSelf = ApplyChatModifiers(ThisSurgeryStep.SuccessSelf);
						string SuccessOther = ApplyChatModifiers(ThisSurgeryStep.SuccessOther);
						string FailSelf = ApplyChatModifiers(ThisSurgeryStep.FailSelf);
						string FailOther = ApplyChatModifiers(ThisSurgeryStep.FailOther);
						ToolUtils.ServerUseToolWithActionMessages(interaction.Performer, interaction.HandObject,
							ActionTarget.Object(interaction.TargetObject.RegisterTile()), ThisSurgeryStep.Time,
							StartSelf, StartOther,
							SuccessSelf, SuccessOther,
							SuccessfulProcedure,
							FailSelf, FailOther,
							UnsuccessfulProcedure);
						return;
					}
					else
					{
						Chat.AddActionMsgToChat(interaction, $" You poke {this.isOn.LivingHealthMasterBase.playerScript.visibleName}'s {RelatedBodyPart.name} with the {interaction.UsedObject.name} ",
							$"{interaction.PerformerPlayerScript.visibleName} pokes {this.isOn.LivingHealthMasterBase.playerScript.visibleName}'s {RelatedBodyPart.name} with the {interaction.UsedObject.name} ");
					}
				}

				if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Cautery))
				{
					Chat.AddActionMsgToChat(interaction, $" You use the {interaction.HandObject.ExpensiveName()} to close up {this.isOn.LivingHealthMasterBase.playerScript.visibleName}'s {RelatedBodyPart.name} ",
						$"{interaction.PerformerPlayerScript.visibleName} uses a {interaction.UsedObject.ExpensiveName()} to close up  {this.isOn.LivingHealthMasterBase.playerScript.visibleName}'s {RelatedBodyPart.name} ");

					CancelSurgery();
					return;
				}
			}

			public void SuccessfulProcedure()
			{
				if (RNG.Next(0, 100) < ThisSurgeryStep.FailChance)
				{
					UnsuccessfulProcedure();
					return;
				}

				CurrentStep++;

				int NumberSteps = 0;
				NumberSteps = SurgeryProcedureBase.SurgerySteps.Count;
				if (CurrentStep >= NumberSteps)
				{
					isOn.ProcedureInProgress = false;

					SurgeryProcedureBase.FinnishSurgeryProcedure(RelatedBodyPart, Stored, this);

					RelatedBodyPart?.SuccessfulProcedure(Stored, this);
					isOn.ProcedureInProgress = false;
					//reset!
				}
			}

			public void UnsuccessfulProcedure()
			{
				SurgeryProcedureBase.UnsuccessfulStep(RelatedBodyPart, Stored, this);

				RelatedBodyPart?.UnsuccessfulStep(Stored, this);
			}

			public void CancelSurgery()
			{
				RelatedBodyPart = PreviousBodyPart;
				isOn.currentlyOn = RelatedBodyPart?.gameObject;
				CurrentStep = 0;
				SurgeryProcedureBase = null;
				isOn.ProcedureInProgress = false;
			}

			public void Clean()
			{
				PreviousBodyPart = RelatedBodyPart;
				RelatedBodyPart = null;
				CurrentStep = 0;
				SurgeryProcedureBase = null;
			}

			public void SetupProcedure(Dissectible Dissectible, BodyPart bodyPart,
				SurgeryProcedureBase inSurgeryProcedureBase)
			{
				Clean();
				if (isOn != Dissectible)
				{
					PreviousBodyPart = null;
				}

				isOn = Dissectible;
				isOn.ProcedureInProgress = true;
				RelatedBodyPart = bodyPart;
				SurgeryProcedureBase = inSurgeryProcedureBase;
			}

			/// <summary>
			/// Replaces $ tags with their wanted names.
			/// </summary>
			/// <param name="toReplace">must have performer, Using and/or OnPart tags to replace.</param>
			/// <returns></returns>
			public string ApplyChatModifiers(string toReplace)
			{
				if (!string.IsNullOrWhiteSpace(toReplace))
				{
					toReplace = toReplace.Replace("{WhoOn}", Stored.TargetObject.ExpensiveName());
				}

				if (!string.IsNullOrWhiteSpace(toReplace))
				{
					toReplace = toReplace.Replace("{performer}", Stored.Performer.ExpensiveName());
				}

				if (!string.IsNullOrWhiteSpace(toReplace))
				{
					toReplace = toReplace.Replace("{Using}", Stored.UsedObject.ExpensiveName());
				}

				if (!string.IsNullOrWhiteSpace(toReplace))
				{
					toReplace = toReplace.Replace("{OnPart}", RelatedBodyPart.OrNull()?.gameObject.OrNull()?.ExpensiveName());
				}

				return toReplace;
			}
		}
	}
}
