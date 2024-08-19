using System.Collections.Generic;
using System.Linq;
using HealthV2.Living.Surgery;
using UnityEngine;
using Mirror;
using Player;
using UI.Systems.Tooltips.HoverTooltips;

namespace HealthV2
{
	public class Dissectible : NetworkBehaviour, IClientInteractable<HandApply>,
		ICheckedInteractable<HandApply>, IExaminable, IHoverTooltip
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

		[field: SyncVar] public string NextTraitToUse { get; set; } = "";

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
			if (ProcedureInProgress) return;
			if (BodyPartIsOn != null)
			{
				UIManager.Instance.SurgeryDialogue.ShowDialogue(this, Options);
			}
			else
			{
				UIManager.Instance.SurgeryDialogue.ShowDialogue(this, Options, true);
			}
		}

		public string Examine(Vector3 worldPos = default)
		{
			if (BodyPartIsopen == false) return null;
			return "<color=red>This body is open.</color>";
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
			List<TextColor> interactions = new List<TextColor>();
			if (ProcedureInProgress && string.IsNullOrEmpty(NextTraitToUse) == false)
			{
				interactions.Add(new TextColor
				{
					Text = $"Use a {NextTraitToUse} to procced with the next step of the surgery.",
					Color = Color.yellow
				});
			}
			if (BodyPartIsopen)
			{
				interactions.Add(new TextColor
				{
					Text = $"Use a {CommonTraits.Instance.Cautery.Name} to close up.",
					Color = Color.green
				});
			}
			return interactions;
		}
	}
}
