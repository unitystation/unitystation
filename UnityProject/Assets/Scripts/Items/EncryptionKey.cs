using System;
using System.Collections.Generic;
using System.Linq;
using Logs;
using UnityEngine;
using Mirror;
using Objects.Telecomms;
using Messages.Client;
using ScriptableObjects.Communications;
using SecureStuff;

namespace Items
{

/// <summary>
///     Encryption Key properties
/// </summary>
public class EncryptionKey : NetworkBehaviour, ICheckedInteractable<HandApply>, ICheckedInteractable<MouseDrop>, IClientInteractable<InventoryApply>
{
	//TODO (Max): Turn this into a list and support multiple encryption data in one key
	public EncryptionDataSO EncryptionDataSo;

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
		{EncryptionKeyType.SrvSec, ChatChannel.Common | ChatChannel.Security | ChatChannel.Service},
		{EncryptionKeyType.SrvMed, ChatChannel.Common | ChatChannel.Medical | ChatChannel.Service},
		{EncryptionKeyType.CentCommPlus, ChatChannel.Common | ChatChannel.Command | ChatChannel.Security | ChatChannel.Engineering |
									ChatChannel.Supply | ChatChannel.Service | ChatChannel.Medical | ChatChannel.Science | ChatChannel.CentComm},
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
		{EncryptionKeyType.SrvSec, ChatChannel.Security},
		{EncryptionKeyType.SrvMed, ChatChannel.Medical},
		{EncryptionKeyType.CentCommPlus, ChatChannel.CentComm}
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
				Loggy.LogError("Encryption keys cannot be None type!", Category.Chat);
				type = EncryptionKeyType.Common;
			}
		}
	}

/// Look ma, no syncvars!
/// This allows clients to initialize attributes
/// without having to resort to SyncVars and ItemFactory (see IDCard example)
/// Downside – all players will get that info (same with syncvars)
	public override void OnSerialize(NetworkWriter writer, bool initialState)
	{
		writer.WriteString(type.ToString());
		base.OnSerialize(writer, initialState);
	}
	public override void OnDeserialize(NetworkReader reader, bool initialState)
	{
		EncryptionKeyType keyType;
		Enum.TryParse(reader.ReadString(),true, out keyType);
		type = keyType;
		base.OnDeserialize(reader, initialState);
	}

	public void onExamine(Vector3 worldPos)
	{
		Chat.AddExamineMsgToClient(ExamineTexts[Type]);
	}

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (DefaultWillInteract.Default(interaction, side) == false) return false;
		if (interaction.TargetObject.TryGetComponent<StationBouncedRadio>(out var _)) return true;
		return false;
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		if (interaction.TargetObject.TryGetComponent<StationBouncedRadio>(out var radio))
		{
			radio.AddEncryptionKey(this);
		}
	}

	public bool WillInteract(MouseDrop interaction, NetworkSide side)
	{
		if (DefaultWillInteract.Default(interaction, side) == false) return false;
		if (interaction.TargetObject.TryGetComponent<StationBouncedRadio>(out var _)) return true;
		return false;
	}

	public void ServerPerformInteraction(MouseDrop interaction)
	{
		if (interaction.TargetObject.TryGetComponent<StationBouncedRadio>(out var radio))
		{
			radio.AddEncryptionKey(this);
		}
	}

	public bool Interact(InventoryApply interaction)
	{
		//insert the headset key if this is used on a headset
		if (interaction.UsedObject == gameObject
		    && interaction.TargetObject.GetComponent<Headset>() != null)
		{
			UpdateHeadsetKeyMessage.Send(interaction.TargetObject, gameObject);
			return true;
		}

		return false;
	}
}
}
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
	CentCommPlus,
	SrvMed,
}
