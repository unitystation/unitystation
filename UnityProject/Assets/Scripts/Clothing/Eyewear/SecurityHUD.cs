using System.Collections;
using System.Collections.Generic;
using Mirror;
using Objects.Security;
using Systems;
using UnityEngine;

public class SecurityHUD : NetworkBehaviour, IHUD
{
	[field: SerializeField] public GameObject Prefab { get; set; }

	public GameObject InstantiatedGameObject { get; set; }

	public HUDHandler HUDHandler;

	private SecurityHUDHandler SecurityHUDHandler;

	public PlayerScript PlayerScript;


	[SyncVar(hook = nameof(SyncCurrentState))]
	public StatusIcon CurrentState = StatusIcon.None;


	[SyncVar(hook = nameof(SyncCurrentJob))]
	public JobIcon CurrentJob = JobIcon.NoID;

	[SyncVar(hook = nameof(SyncCurrentImplant))]
	public bool HasImplant = false;

	public void SyncCurrentState(StatusIcon OldStatus, StatusIcon NewStatus)
	{
		CurrentState = NewStatus;
		SecurityHUDHandler.StatusIcon.SetCatalogueIndexSprite((int) CurrentState);
	}

	public void SyncCurrentJob(JobIcon OldJob, JobIcon NewJob)
	{
		CurrentJob = NewJob;
		SecurityHUDHandler.RoleIcon.SetCatalogueIndexSprite((int) CurrentJob);
	}

	public void SyncCurrentImplant(bool OldImplant, bool NewImplant)
	{
		HasImplant = NewImplant;
		SecurityHUDHandler.MindShieldImplant.SetCatalogueIndexSprite(HasImplant ? 1 : 0);
	}

	public void Awake()
	{
		PlayerScript = this.GetComponentCustom<PlayerScript>();
		HUDHandler = this.GetComponentCustom<HUDHandler>();


		if (CustomNetworkManager.IsServer)
		{
			PlayerScript.DynamicItemStorage.OnContentsChangeServer.AddListener(JobChange);
			PlayerScript.OnVisibleNameChange += JobChange; //TODO Is not the best place
			PlayerScript.OnVisibleNameChange += IdentityChange;
			SecurityRecord.OnWantedLevelChange += IdentityChange; //TODO This could be a bit better as well
		}

		HUDHandler.AddNewHud(this);
	}


	public void IdentityChange()
	{
		var name = PlayerScript.visibleName;
		//Look up in database


		StatusIcon NewStatusIcon = StatusIcon.None;

		//Magic
		if (string.IsNullOrEmpty(name) == false && CrewManifestManager.Instance.NameLookUpSecurityRecords.ContainsKey(name))
		{
			var Listy = CrewManifestManager.Instance.NameLookUpSecurityRecords[name];
			foreach (var Record in Listy)
			{
				if (NewStatusIcon == StatusIcon.None && Record.Status != SecurityStatus.None)
				{
					NewStatusIcon = SecurityStatusToIcon(Record.Status);
				}
				else if (NewStatusIcon == StatusIcon.Paroled && Record.Status != SecurityStatus.None
				                                             && Record.Status != SecurityStatus.Parole
				                                             )
				{
					NewStatusIcon = SecurityStatusToIcon(Record.Status);
				}
				else if (NewStatusIcon == StatusIcon.Discharged && Record.Status != SecurityStatus.None
				                                                && Record.Status != SecurityStatus.Parole)
				{
					NewStatusIcon = SecurityStatusToIcon(Record.Status);
				}
				else if (NewStatusIcon == StatusIcon.Incarcerated && Record.Status != SecurityStatus.None
				                                                 && Record.Status != SecurityStatus.Parole
				                                                 && Record.Status != SecurityStatus.Criminal)
				{
					NewStatusIcon = SecurityStatusToIcon(Record.Status);
				}
				else if (NewStatusIcon == StatusIcon.Wanted)
				{
					break;
				}
			}


		}

		SyncCurrentState(CurrentState, NewStatusIcon);
	}


	public void JobChange()
	{
		var NewJobType = JobType.NULL;

		foreach (var itemSlot in PlayerScript.DynamicItemStorage.GetNamedItemSlots(NamedSlot.id))
		{
			if (itemSlot.Item == null) continue;
			if (itemSlot.Item.TryGetComponent<IDCard>(out var idCard))
			{
				NewJobType = idCard.JobType;
				break;
			}
			else if (itemSlot.Item.TryGetComponent<Items.PDA.PDALogic>(out var pda))
			{
				idCard = pda.GetIDCard();
				if (idCard == null) continue;
				NewJobType = idCard.JobType;
				break;
			}
		}

		var newJobIcon = JobTypeToIcon(NewJobType);

		//Look up in database
		SyncCurrentJob(CurrentJob, newJobIcon);
	}


	public StatusIcon SecurityStatusToIcon(SecurityStatus securityStatus)
	{
		switch (securityStatus)
		{
			case SecurityStatus.None:
				return StatusIcon.None;
			case SecurityStatus.Arrest:
				return StatusIcon.Wanted;
			case SecurityStatus.Criminal:
				return StatusIcon.Incarcerated;
			case SecurityStatus.Parole:
				return StatusIcon.Released;
			default:
				return StatusIcon.Paroled;
		}

	}

	public JobIcon JobTypeToIcon(JobType JobType)
	{
		switch (JobType)
		{
			case JobType.NULL:
				return JobIcon.NoID;
			case JobType.ASSISTANT:
				return JobIcon.Assistant;
			case JobType.ATMOSTECH:
				return JobIcon.AtmosphericTechnician;
			case JobType.BARTENDER:
				return JobIcon.Bartender;
			case JobType.BOTANIST:
				return JobIcon.Botanist;
			case JobType.CAPTAIN:
				return JobIcon.Captain;
			case JobType.CARGOTECH:
				return JobIcon.CargoTech;
			case JobType.CENTCOMM_INTERN:
			case JobType.CENTCOMM_OFFICER:
			case JobType.CENTCOMM_COMMANDER:
				return JobIcon.Centcom;
			case JobType.CHAPLAIN:
				return JobIcon.Chaplin;
			case JobType.CHEMIST:
				return JobIcon.Chemist;
			case JobType.CHIEF_ENGINEER:
				return JobIcon.ChiefEngineer;
			case JobType.CMO:
				return JobIcon.ChiefMedicalOfficer;
			case JobType.CLOWN:
				return JobIcon.Clown;
			case JobType.COOK:
				return JobIcon.Cook;
			case JobType.CURATOR:
				return JobIcon.Curator;
			case JobType.DETECTIVE:
				return JobIcon.Detective;
			case JobType.GENETICIST:
				return JobIcon.Geneticist;
			case JobType.HOP:
				return JobIcon.HeadOfPersonnel;
			case JobType.HOS:
				return JobIcon.HeadOfSecurity;
			case JobType.JANITOR:
				return JobIcon.Janitor;
			case JobType.LAWYER:
				return JobIcon.Lawyer;
			case JobType.DOCTOR:
				return JobIcon.Medic;
			case JobType.MIME:
				return JobIcon.Mime;
			case JobType.PRISONER:
				return JobIcon.Prisoner;
			case JobType.QUARTERMASTER:
				return JobIcon.QuarterMaster;
			case JobType.RD:
				return JobIcon.ResearchDirector;
			case JobType.ROBOTICIST:
				return JobIcon.Roboticist;
			case JobType.SCIENTIST:
				return JobIcon.Scientist;
			case JobType.SECURITY_OFFICER:
				return JobIcon.Security;
			case JobType.MINER:
				return JobIcon.ShaftMiner;

			case JobType.ENGINEER:
				return JobIcon.StationEngineer;

			case JobType.VIROLOGIST:
				return JobIcon.Virologist;

			case JobType.WARDEN:
				return JobIcon.Warden;
			default:
				return JobIcon.Unknown;
		}
	}



	public void SetUp()
	{
		SecurityHUDHandler = InstantiatedGameObject.GetComponent<SecurityHUDHandler>();
		SecurityHUDHandler.StatusIcon.SetCatalogueIndexSprite((int) CurrentState);
		SecurityHUDHandler.RoleIcon.SetCatalogueIndexSprite((int) CurrentJob);
		SecurityHUDHandler.MindShieldImplant.SetCatalogueIndexSprite(HasImplant ? 1 : 0);

		var visibility = false;
		var ThisType = typeof(SecurityHUD);
		if (HUDHandler.CategoryEnabled.ContainsKey(ThisType)) //So if you join mid round you still have the HUD showing
		{
			visibility = HUDHandler.CategoryEnabled[ThisType];
		}
		SecurityHUDHandler.SetVisible(visibility);
	}

	public void SetVisible(bool visible)
	{
		SecurityHUDHandler.SetVisible(visible);
	}


	public void OnDestroy()
	{
		HUDHandler.RemoveHud(this);

		PlayerScript.OnVisibleNameChange -= JobChange;
		PlayerScript.OnVisibleNameChange -= IdentityChange;
		SecurityRecord.OnWantedLevelChange -= IdentityChange;
	}

	public enum StatusIcon
	{
		None,
		Discharged,
		Incarcerated,
		Paroled,
		Released,
		Wanted
	}

	public enum JobIcon
	{
		Assistant,
		AtmosphericTechnician,
		Bartender,
		Botanist,
		Captain,
		CargoTech,
		Centcom,
		Chaplin,
		Chef,
		Chemist,
		ChiefEngineer,
		ChiefMedicalOfficer,
		Clown,
		Cook,
		Curator,
		Detective,
		Geneticist,
		HeadOfPersonnel,
		HeadOfSecurity,
		HeadOfSecurityOld,
		Janitor,
		Lawyer,
		Medic,
		Mime,
		NoID,
		Prisoner,
		QuarterMaster,
		ResearchDirector,
		Roboticist,
		Scientist,
		Security,
		SecurityOld,
		ShaftMiner,
		StationEngineer,
		Unknown,
		Virologist,
		Warden
	}
}