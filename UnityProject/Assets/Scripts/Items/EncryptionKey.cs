using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public enum EncryptionKeyType
{
	None, //For when headsets don't have any key inside. Key itself cannot be this type.
	Common,
	Medical,
	Science,
	Service,
	Security,
	Supply,
	QuarterMaster,
	Engineering,
	HeadOfPersonnel,
	Captain,
	ResearchDirector,
	HeadOfSecurity,
	ChiefEngineer,
	ChiefMedicalOfficer,
	Binary,
	Syndicate,
	CentComm
}

/// <summary>
/// Encryption Key properties
/// </summary>
public class EncryptionKey : NetworkBehaviour
{
    //So that we don't have to create a different item for each type
    public SpriteRenderer spriteRenderer;
    public Sprite commonSprite;
	public Sprite medicalSprite;
	public Sprite scienceSprite;
	public Sprite serviceSprite;
	public Sprite securitySprite;
	public Sprite supplySprite;
	public Sprite quarterMasterSprite;
	public Sprite engineeringSprite;
	public Sprite headOfPersonnelSprite;
	public Sprite captainSprite;
	public Sprite researchDirectorSprite;
	public Sprite headOfSecuritySprite;
	public Sprite chiefEngineerSprite;
	public Sprite chiefMedicalOfficerSprite;
	public Sprite binarySprite;
	public Sprite syndicateSprite;
	public Sprite centCommSprite;

	public static readonly Dictionary<EncryptionKeyType, ChatChannel> Permissions
		= new Dictionary<EncryptionKeyType, ChatChannel>
	{
		{ EncryptionKeyType.None, ChatChannel.None },
		{ EncryptionKeyType.Common, ChatChannel.Common },
		{ EncryptionKeyType.Binary, ChatChannel.Common | ChatChannel.Binary },
		{ EncryptionKeyType.Captain, ChatChannel.Common | ChatChannel.Command | ChatChannel.Security | ChatChannel.Engineering | ChatChannel.Supply | ChatChannel.Service | ChatChannel.Medical | ChatChannel.Science },
		{ EncryptionKeyType.ChiefEngineer, ChatChannel.Common | ChatChannel.Engineering | ChatChannel.Command },
		{ EncryptionKeyType.ChiefMedicalOfficer, ChatChannel.Common | ChatChannel.Medical | ChatChannel.Command },
		{ EncryptionKeyType.HeadOfPersonnel, ChatChannel.Common | ChatChannel.Supply | ChatChannel.Service | ChatChannel.Command },
		{ EncryptionKeyType.HeadOfSecurity, ChatChannel.Common | ChatChannel.Security | ChatChannel.Command },
		{ EncryptionKeyType.ResearchDirector, ChatChannel.Common | ChatChannel.Science | ChatChannel.Command },
		{ EncryptionKeyType.Supply, ChatChannel.Common | ChatChannel.Supply },
		{ EncryptionKeyType.QuarterMaster, ChatChannel.Common | ChatChannel.Supply | ChatChannel.Command },
		{ EncryptionKeyType.CentComm, ChatChannel.Common | ChatChannel.CentComm },
		{ EncryptionKeyType.Engineering, ChatChannel.Common | ChatChannel.Engineering },
		{ EncryptionKeyType.Medical, ChatChannel.Common | ChatChannel.Medical },
		{ EncryptionKeyType.Science, ChatChannel.Common | ChatChannel.Science },
		{ EncryptionKeyType.Security, ChatChannel.Common | ChatChannel.Security },
		{ EncryptionKeyType.Service, ChatChannel.Common | ChatChannel.Service },
		{ EncryptionKeyType.Syndicate, ChatChannel.Common | ChatChannel.Syndicate },
	};

	[SerializeField]//to show in inspector
	private EncryptionKeyType type;

	private static readonly Dictionary<EncryptionKeyType, string> ExamineTexts
	= new Dictionary<EncryptionKeyType, string>
	{
		{ EncryptionKeyType.Common, "An encryption key for a radio headset. \nHas no special codes in it.  WHY DOES IT EXIST?  ASK NANOTRASEN." },
		{ EncryptionKeyType.Binary, "An encryption key for a radio headset. \nTo access the binary channel, use :b." },
		{ EncryptionKeyType.Captain, "An encryption key for a radio headset. \nChannels are as follows: :c - command, :s - security, :e - engineering, :u - supply, :v - service, :m - medical, :n - science." },
		{ EncryptionKeyType.ChiefEngineer, "An encryption key for a radio headset. \nTo access the engineering channel, use :e. For command, use :c." },
		{ EncryptionKeyType.ChiefMedicalOfficer, "An encryption key for a radio headset. \nTo access the medical channel, use :m. For command, use :c." },
		{ EncryptionKeyType.HeadOfPersonnel, "An encryption key for a radio headset. \nChannels are as follows: :u - supply, :v - service, :c - command." },
		{ EncryptionKeyType.HeadOfSecurity, "An encryption key for a radio headset. \nTo access the security channel, use :s. For command, use :c." },
		{ EncryptionKeyType.ResearchDirector, "An encryption key for a radio headset. \nTo access the science channel, use :n. For command, use :c." },
		{ EncryptionKeyType.Supply, "An encryption key for a radio headset. \nTo access the supply channel, use :u." },
		{ EncryptionKeyType.QuarterMaster, "An encryption key for a radio headset. \nTo access the supply channel, use :u. For command, use :c." },
		{ EncryptionKeyType.CentComm, "An encryption key for a radio headset. \nTo access the centcom channel, use :y." },
		{ EncryptionKeyType.Engineering, "An encryption key for a radio headset.  \nTo access the engineering channel, use :e." },
		{ EncryptionKeyType.Medical, "An encryption key for a radio headset. \nTo access the medical channel, use :m." },
		{ EncryptionKeyType.Science, "An encryption key for a radio headset. \nTo access the science channel, use :n." },
		{ EncryptionKeyType.Security, "An encryption key for a radio headset. \nTo access the security channel, use :s." },
		{ EncryptionKeyType.Service, "An encryption key for a radio headset.  \nTo access the service channel, use :v." },
		{ EncryptionKeyType.Syndicate, "An encryption key for a radio headset.\nTo access the syndicate channel, use :t." },
	};

	private void Start()
	{
		UpdateSprite();
	}

	public EncryptionKeyType Type
	{
		get { return type; }
		set {
			type = value;
			if(type == EncryptionKeyType.None) {
				Debug.LogError("Encryption keys cannot be None type!");
				type = EncryptionKeyType.Common;
			}
			UpdateSprite();
		}
	}

	#region Set the sprite based on key type
	private void UpdateSprite()
    {
		switch(type) {
			case EncryptionKeyType.Binary:
				spriteRenderer.sprite = binarySprite;
				break;
			case EncryptionKeyType.Captain:
				spriteRenderer.sprite = captainSprite;
				break;
			case EncryptionKeyType.Supply:
				spriteRenderer.sprite = supplySprite;
				break;
			case EncryptionKeyType.CentComm:
				spriteRenderer.sprite = centCommSprite;
				break;
			case EncryptionKeyType.ChiefEngineer:
				spriteRenderer.sprite = chiefEngineerSprite;
				break;
			case EncryptionKeyType.ChiefMedicalOfficer:
				spriteRenderer.sprite = chiefMedicalOfficerSprite;
				break;
			case EncryptionKeyType.Common:
				spriteRenderer.sprite = commonSprite;
				break;
			case EncryptionKeyType.Engineering:
				spriteRenderer.sprite = engineeringSprite;
				break;
			case EncryptionKeyType.HeadOfPersonnel:
				spriteRenderer.sprite = headOfPersonnelSprite;
				break;
			case EncryptionKeyType.HeadOfSecurity:
				spriteRenderer.sprite = headOfSecuritySprite;
				break;
			case EncryptionKeyType.Medical:
				spriteRenderer.sprite = medicalSprite;
				break;
			case EncryptionKeyType.QuarterMaster:
				spriteRenderer.sprite = quarterMasterSprite;
				break;
			case EncryptionKeyType.ResearchDirector:
				spriteRenderer.sprite = researchDirectorSprite;
				break;
			case EncryptionKeyType.Science:
				spriteRenderer.sprite = scienceSprite;
				break;
			case EncryptionKeyType.Security:
				spriteRenderer.sprite = securitySprite;
				break;
			case EncryptionKeyType.Service:
				spriteRenderer.sprite = serviceSprite;
				break;
			case EncryptionKeyType.Syndicate:
				spriteRenderer.sprite = syndicateSprite;
				break;
			default:
				spriteRenderer.sprite = commonSprite;
				break;
		}
    }
#endregion

	public void OnExamine()
	{
		UI.UIManager.Chat.AddChatEvent(new ChatEvent(ExamineTexts[Type], ChatChannel.Examine));
	}
}
