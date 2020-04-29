using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Mirror;

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
	CentComm,
	Mining,
	Genetics,
	SrvSec,
}

/// <summary>
///     Encryption Key properties
/// </summary>
public class EncryptionKey : NetworkBehaviour
{
	public static readonly Dictionary<EncryptionKeyType, ChatChannel> Permissions = new Dictionary<EncryptionKeyType, ChatChannel>
	{
		{EncryptionKeyType.None, ChatChannel.None},
		{EncryptionKeyType.Common, ChatChannel.Common},
		{EncryptionKeyType.Binary, ChatChannel.Common | ChatChannel.Binary},
		{EncryptionKeyType.Captain, ChatChannel.Common | ChatChannel.Command | ChatChannel.Security | ChatChannel.Engineering |
									ChatChannel.Supply | ChatChannel.Service | ChatChannel.Medical | ChatChannel.Science},
		{EncryptionKeyType.ChiefEngineer, ChatChannel.Common | ChatChannel.Engineering | ChatChannel.Command},
		{EncryptionKeyType.ChiefMedicalOfficer, ChatChannel.Common | ChatChannel.Medical | ChatChannel.Command},
		{EncryptionKeyType.HeadOfPersonnel, ChatChannel.Common | ChatChannel.Supply | ChatChannel.Service | ChatChannel.Command},
		{EncryptionKeyType.HeadOfSecurity, ChatChannel.Common | ChatChannel.Security | ChatChannel.Command},
		{EncryptionKeyType.ResearchDirector, ChatChannel.Common | ChatChannel.Science | ChatChannel.Command},
		{EncryptionKeyType.Supply, ChatChannel.Common | ChatChannel.Supply},
		{EncryptionKeyType.QuarterMaster, ChatChannel.Common | ChatChannel.Supply},
		{EncryptionKeyType.CentComm, ChatChannel.Common | ChatChannel.CentComm},
		{EncryptionKeyType.Engineering, ChatChannel.Common | ChatChannel.Engineering},
		{EncryptionKeyType.Medical, ChatChannel.Common | ChatChannel.Medical},
		{EncryptionKeyType.Science, ChatChannel.Common | ChatChannel.Science},
		{EncryptionKeyType.Security, ChatChannel.Common | ChatChannel.Security},
		{EncryptionKeyType.Service, ChatChannel.Common | ChatChannel.Service},
		{EncryptionKeyType.Syndicate, ChatChannel.Syndicate },
		{EncryptionKeyType.Mining, ChatChannel.Common | ChatChannel.Supply | ChatChannel.Science},
		{EncryptionKeyType.Genetics, ChatChannel.Common | ChatChannel.Medical | ChatChannel.Science},
		{EncryptionKeyType.SrvSec, ChatChannel.Common | ChatChannel.Security | ChatChannel.Service}
	};

	/// <summary>
	/// Default department channel (tag ':h') by different encryption keys
	/// </summary>
	public static readonly Dictionary<EncryptionKeyType, ChatChannel> DefaultChannel = new Dictionary<EncryptionKeyType, ChatChannel>()
	{
		{EncryptionKeyType.Binary, ChatChannel.Binary},
		{EncryptionKeyType.Captain, ChatChannel.Command },
		{EncryptionKeyType.CentComm, ChatChannel.CentComm },
		{EncryptionKeyType.ChiefEngineer, ChatChannel.Engineering },
		{EncryptionKeyType.ChiefMedicalOfficer, ChatChannel.Medical },
		{EncryptionKeyType.Common, ChatChannel.Common },
		{EncryptionKeyType.Engineering, ChatChannel.Engineering },
		{EncryptionKeyType.Genetics, ChatChannel.Science },
		{EncryptionKeyType.HeadOfPersonnel, ChatChannel.Service },
		{EncryptionKeyType.HeadOfSecurity, ChatChannel.Security },
		{EncryptionKeyType.Medical, ChatChannel.Medical },
		{EncryptionKeyType.Mining, ChatChannel.Supply },
		{EncryptionKeyType.None, ChatChannel.None },
		{EncryptionKeyType.QuarterMaster, ChatChannel.Supply },
		{EncryptionKeyType.ResearchDirector, ChatChannel.Science },
		{EncryptionKeyType.Science, ChatChannel.Science },
		{EncryptionKeyType.Security, ChatChannel.Security },
		{EncryptionKeyType.Service, ChatChannel.Service },
		{EncryptionKeyType.Supply, ChatChannel.Supply },
		{EncryptionKeyType.Syndicate, ChatChannel.Syndicate },
		{EncryptionKeyType.SrvSec, ChatChannel.Security}
	};

	private static readonly string genericDescription = "An encryption key for a radio headset. \n";

	private static readonly Dictionary<EncryptionKeyType, string> ExamineTexts = new Dictionary<EncryptionKeyType, string>
	{
		{EncryptionKeyType.Common, 				$"{genericDescription}Has no special codes in it.  WHY DOES IT EXIST?  ASK NANOTRASEN."},
		{EncryptionKeyType.Binary, 				$"{hotkeyDescription(EncryptionKeyType.Binary)}"},
		{EncryptionKeyType.Captain, 			$"{hotkeyDescription(EncryptionKeyType.Captain)}"},
		{EncryptionKeyType.ChiefEngineer, 		$"{hotkeyDescription(EncryptionKeyType.ChiefEngineer)}"},
		{EncryptionKeyType.ChiefMedicalOfficer, $"{hotkeyDescription(EncryptionKeyType.ChiefMedicalOfficer)}"},
		{EncryptionKeyType.HeadOfPersonnel, 	$"{hotkeyDescription(EncryptionKeyType.HeadOfPersonnel)}"},
		{EncryptionKeyType.HeadOfSecurity, 		$"{hotkeyDescription(EncryptionKeyType.HeadOfSecurity)}"},
		{EncryptionKeyType.ResearchDirector, 	$"{hotkeyDescription(EncryptionKeyType.ResearchDirector)}"},
		{EncryptionKeyType.Supply, 				$"{hotkeyDescription(EncryptionKeyType.Supply)}"},
		{EncryptionKeyType.QuarterMaster, 		$"{hotkeyDescription(EncryptionKeyType.QuarterMaster)}"},
		{EncryptionKeyType.CentComm, 			$"{hotkeyDescription(EncryptionKeyType.CentComm)}"},
		{EncryptionKeyType.Engineering, 		$"{hotkeyDescription(EncryptionKeyType.Engineering)}"},
		{EncryptionKeyType.Medical, 			$"{hotkeyDescription(EncryptionKeyType.Medical)}"},
		{EncryptionKeyType.Science, 			$"{hotkeyDescription(EncryptionKeyType.Science)}"},
		{EncryptionKeyType.Security, 			$"{hotkeyDescription(EncryptionKeyType.Security)}"},
		{EncryptionKeyType.Service, 			$"{hotkeyDescription(EncryptionKeyType.Service)}"},
		{EncryptionKeyType.Syndicate, 			$"{hotkeyDescription(EncryptionKeyType.Syndicate)}"}
	};

	/// Generates a description for headset encryption key type
	private static string hotkeyDescription(EncryptionKeyType keyType)
	{
		List<ChatChannel> chatChannels = getChannelList(keyType);
		return $"{genericDescription}{string.Join(",\n",getChannelDescriptions(chatChannels))}";
	}

	/// string representation of channels and theor hotkeys
	private static List<string> getChannelDescriptions(List<ChatChannel> channels)
	{
		List<string> descriptions = new List<string>();
		for ( var i = 0; i < channels.Count; i++ )
		{
			descriptions.Add($"{channels[i].ToString()} — {channels[i].GetDescription()}");
		}
		return descriptions;
	}

	/// For dumb people like me who can't do bitwise. Better cache these
	private static List<ChatChannel> getChannelList(EncryptionKeyType keyType)
	{
		ChatChannel channelMask = Permissions[keyType];
		return getChannelsByMask(channelMask);
	}

	public static List<ChatChannel> getChannelsByMask(ChatChannel channelMask)
	{
		List<ChatChannel> channelsByMask = Enum.GetValues(typeof( ChatChannel ))
			.Cast<ChatChannel>()
			.Where(value => channelMask.HasFlag(value))
			.ToList();
		channelsByMask.Remove(ChatChannel.None);
		return channelsByMask;
	}

	public Sprite binarySprite;
	public Sprite captainSprite;
	public Sprite centCommSprite;
	public Sprite chiefEngineerSprite;
	public Sprite chiefMedicalOfficerSprite;

	public Sprite commonSprite;
	public Sprite engineeringSprite;
	public Sprite headOfPersonnelSprite;
	public Sprite headOfSecuritySprite;
	public Sprite medicalSprite;
	public Sprite quarterMasterSprite;
	public Sprite researchDirectorSprite;
	public Sprite scienceSprite;
	public Sprite securitySprite;

	public Sprite serviceSprite;

	//So that we don't have to create a different item for each type
	public SpriteRenderer spriteRenderer;

	public Sprite supplySprite;
	public Sprite syndicateSprite;
	public Sprite srvsecSprite;

	[SerializeField] //to show in inspector
	private EncryptionKeyType type;

	public EncryptionKeyType Type
	{
		get { return type; }
		set
		{
			type = value;
			if (type == EncryptionKeyType.None)
			{
				Logger.LogError("Encryption keys cannot be None type!", Category.Telecoms);
				type = EncryptionKeyType.Common;
			}
			UpdateSprite();
		}
	}

	private void Start()
	{
		UpdateSprite();
	}

/// Look ma, no syncvars!
/// This allows clients to initialize attributes
/// without having to resort to SyncVars and ItemFactory (see IDCard example)
/// Downside – all players will get that info (same with syncvars)
	public override bool OnSerialize(NetworkWriter writer, bool initialState)
	{
		writer.WriteString(type.ToString());
		return base.OnSerialize(writer, initialState);
	}
	public override void OnDeserialize(NetworkReader reader, bool initialState)
	{
		EncryptionKeyType keyType;
		Enum.TryParse(reader.ReadString(),true, out keyType);
		type = keyType;
		base.OnDeserialize(reader, initialState);
	}

	#region Set the sprite based on key type

	private void UpdateSprite()
	{
		switch (type)
		{
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
			case EncryptionKeyType.SrvSec:
				spriteRenderer.sprite = srvsecSprite;
				break;
			default:
				spriteRenderer.sprite = commonSprite;
				break;
		}
	}

	#endregion

	public void onExamine(Vector3 worldPos)
	{
		Chat.AddExamineMsgToClient(ExamineTexts[Type]);
	}
}