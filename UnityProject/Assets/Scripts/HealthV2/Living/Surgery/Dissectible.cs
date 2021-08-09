using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace HealthV2
{
	public class Dissectible : NetworkBehaviour, IClientInteractable<PositionalHandApply>,
		ICheckedInteractable<PositionalHandApply>
	{
		public LivingHealthMasterBase LivingHealthMasterBase;

		//needs set Surgery procedure

		[SyncVar(hook = nameof(SetProcedureInProgress))]
		private bool ProcedureInProgress = false;

		private GameObject InternalcurrentlyOn = null;


		public GameObject currentlyOn
		{
			get
			{
				if (isServer)
				{
					return InternalcurrentlyOn;
				}
				else
				{
					if (NetworkIdentity.spawned.ContainsKey(BodyPartID) && NetworkIdentity.spawned[BodyPartID] != null)
					{
						return NetworkIdentity.spawned[BodyPartID].gameObject;
					}

					return null;
				}
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

		public List<ItemTrait>
			InitiateSurgeryItemTraits = new List<ItemTrait>(); //Make sure to include implantable stuff


		public bool WillInteract(PositionalHandApply interaction, NetworkSide side)
		{
			if (interaction.Intent != Intent.Help) return false; //TODO problem with surgery in Progress and Trying to use something on help content on them
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			if (ProcedureInProgress == false) return false;
			var RegisterPlayer = interaction.TargetObject.GetComponent<RegisterPlayer>();

			if (RegisterPlayer == null) return false; //Player script changes needed
			if (RegisterPlayer.IsLayingDown == false) return false;
			return (true);
		}


		public void ServerPerformInteraction(PositionalHandApply interaction)
		{
			if (ProcedureInProgress)
			{
				ThisPresentProcedure.TryTool(interaction);
			}
		}

		public bool Interact(PositionalHandApply interaction)
		{
			if (DefaultWillInteract.Default(interaction, NetworkSide.Client) == false) return false;
			//**Client**
			if (Validations.HasComponent<Dissectible>(interaction.TargetObject) == false) return false;

			var RegisterPlayer = interaction.TargetObject.GetComponent<RegisterPlayer>();

			if (RegisterPlayer == null) return false; //Player script changes needed


			if (RegisterPlayer.IsLayingDown == false) return false;

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
						// var Options = currentlyOn.ContainBodyParts;
						// UIManager.Instance.SurgeryDialogue.ShowDialogue(this, Options);
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
					// var Options = LivingHealthMasterBase.GetBodyPartsInZone(interaction.TargetBodyPart);
					// UIManager.Instance.SurgeryDialogue.ShowDialogue(this, Options, true);
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


		public void ServerCheck(SurgeryProcedureBase SurgeryProcedureBase, BodyPart ONBodyPart)
		{
			if (ProcedureInProgress)
				return; //Defer to server

			if (BodyPartIsOn != null)
			{
				if (BodyPartIsopen)
				{
					foreach (var inBodyPart in BodyPartIsOn.OrganList)
					{
						//TODO: remove bodypart component from organs
						var organBodyPart = inBodyPart.GetComponent<BodyPart>();
						if (organBodyPart == ONBodyPart)
						{
							foreach (var Procedure in organBodyPart.SurgeryProcedureBase)
							{
								if (Procedure is CloseProcedure || Procedure is ImplantProcedure) continue;
								if (SurgeryProcedureBase == Procedure)
								{
									this.currentlyOn = organBodyPart.gameObject;
									this.ThisPresentProcedure.SetupProcedure(this, organBodyPart, Procedure);
									return;
								}
							}

							return;
						}
					}

					if (currentlyOn == ONBodyPart.gameObject)
					{
						foreach (var Procedure in BodyPartIsOn.SurgeryProcedureBase)
						{
							if (Procedure is CloseProcedure || Procedure is ImplantProcedure)
							{
								if (SurgeryProcedureBase == Procedure)
								{
									this.currentlyOn = currentlyOn;
									this.ThisPresentProcedure.SetupProcedure(this, BodyPartIsOn, Procedure);
									return;
								}
							}
						}

						return;
					}
				}
				else
				{
					if (ONBodyPart.gameObject == currentlyOn)
					{
						foreach (var Procedure in BodyPartIsOn.SurgeryProcedureBase)
						{
							if (Procedure is CloseProcedure || Procedure is ImplantProcedure) continue;
							if (SurgeryProcedureBase == Procedure)
							{
								this.currentlyOn = currentlyOn;
								this.ThisPresentProcedure.SetupProcedure(this, BodyPartIsOn, Procedure);
								return;
							}
						}

						return;
					}
				}
			}
			else
			{
				foreach (var bodyPart in LivingHealthMasterBase.BodyPartList)
				{
					if (bodyPart == ONBodyPart)
					{
						foreach (var Procedure in bodyPart.SurgeryProcedureBase)
						{
							if (Procedure is CloseProcedure || Procedure is ImplantProcedure) continue;
							if (SurgeryProcedureBase == Procedure)
							{
								this.currentlyOn = bodyPart.gameObject;
								this.ThisPresentProcedure.SetupProcedure(this, bodyPart, Procedure);
								return;
							}
						}
					}
				}

				if (LivingHealthMasterBase.GetComponent<PlayerSprites>().RaceBodyparts.Base.RootImplantProcedure ==
				    SurgeryProcedureBase)
				{
					this.currentlyOn = LivingHealthMasterBase.gameObject;
					this.ThisPresentProcedure.SetupProcedure(this, null, SurgeryProcedureBase);
				}
			}

		}

		public void SendClientBodyParts(ConnectedPlayer SentByPlayer, BodyPartType inTargetBodyPart = BodyPartType.None)
		{
			if (currentlyOn == null)
			{
				var targetedBodyParts = new List<BodyPart>();
				foreach (var bodyPart in LivingHealthMasterBase.BodyPartList)
				{
					targetedBodyParts.Add(bodyPart);
				}
				SendSurgeryBodyParts.SendTo(targetedBodyParts, this, SentByPlayer);
			}
			else
			{
				if (BodyPartIsopen)
				{
					//TODO: remove bodypart component from organs
					var organBodyPartList = new List<BodyPart>();
					foreach (var organ in BodyPartIsOn.OrganList)
					{
						organBodyPartList.Add(organ.GetComponent<BodyPart>());
					}
					SendSurgeryBodyParts.SendTo(organBodyPartList, this, SentByPlayer);
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

			public Dissectible ISon;

			public SurgeryProcedureBase SurgeryProcedureBase;
			public int CurrentStep = 0;
			public BodyPart RelatedBodyPart;

			//Used for when surgeries are cancelled
			public BodyPart PreviousBodyPart;


			public PositionalHandApply Stored;
			public SurgeryStep ThisSurgeryStep;


			public void TryTool(PositionalHandApply interaction)
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
				}

				if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Cautery))
				{
					CancelSurgery();
				}
			}

			public void SuccessfulProcedure()
			{
				if (RNG.Next(0, 100) > ThisSurgeryStep.SuccessChance)
				{
					UnsuccessfulProcedure();
					return;
				}

				CurrentStep++;

				int NumberSteps = 0;
				NumberSteps = SurgeryProcedureBase.SurgerySteps.Count;
				if (CurrentStep >= NumberSteps)
				{
					ISon.ProcedureInProgress = false;

					SurgeryProcedureBase.FinnishSurgeryProcedure(RelatedBodyPart, Stored, this);

					RelatedBodyPart?.SuccessfulProcedure(Stored, this);
					ISon.ProcedureInProgress = false;
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
				ISon.currentlyOn = RelatedBodyPart?.gameObject;
				CurrentStep = 0;
				SurgeryProcedureBase = null;
				ISon.ProcedureInProgress = false;
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
				if (ISon != Dissectible)
				{
					PreviousBodyPart = null;
				}

				ISon = Dissectible;
				ISon.ProcedureInProgress = true;
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



		public enum ProcedureType
		{
			Close,
			Custom
		}
	}
}