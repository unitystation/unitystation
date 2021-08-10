using Items;
using Mirror;

/// <summary>
///     Headset properties
/// </summary>
public class Headset : NetworkBehaviour, ICheckedInteractable<HandApply>
{
	[SyncVar] public EncryptionKeyType EncryptionKey;
	public bool IsLoudHeadset = false;
	[SyncVar] public bool LoudHeadsetEnabled = false;

	public void init()
	{
		getEncryptionTypeFromHier();
	}

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if(interaction.IsAltClick) return true;
		if(interaction.IsAltClick && interaction.HandObject == gameObject) return true;
		return false;
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		if(IsLoudHeadset)
		{
			LoudHeadsetEnabled = !LoudHeadsetEnabled;
			if(LoudHeadsetEnabled)
			{
				Chat.AddActionMsgToChat(interaction.Performer, "You've enabled the headset's amplifer. You'll be heard louder on comms from now on.", "");
				_ = SoundManager.Play(SingletonSOSounds.Instance.Click01);
				return;
			}
			Chat.AddActionMsgToChat(interaction.Performer, "You've disabled the headset's amplifer. You'll be heard louder on comms from now on.", "");
		}
	}

	private void getEncryptionTypeFromHier()
	{
		ItemAttributesV2 attr = GetComponent<ItemAttributesV2>();

		//switch (attr.hierarchy)
		//{
		//	case "/obj/item/device/radio/headset":
		//		EncryptionKey = EncryptionKeyType.Common;
		//		break;
		//	case "/obj/item/device/radio/headset/heads/captain":
		//	case "/obj/item/device/radio/headset/heads/captain/alt":
		//		EncryptionKey = EncryptionKeyType.Captain;
		//		break;
		//	case "/obj/item/device/radio/headset/heads/ce":
		//		EncryptionKey = EncryptionKeyType.ChiefEngineer;
		//		break;
		//	case "/obj/item/device/radio/headset/heads/cmo":
		//		EncryptionKey = EncryptionKeyType.ChiefMedicalOfficer;
		//		break;
		//	case "/obj/item/device/radio/headset/heads/hop":
		//		EncryptionKey = EncryptionKeyType.HeadOfPersonnel;
		//		break;
		//	case "/obj/item/device/radio/headset/heads/hos":
		//	case "/obj/item/device/radio/headset/heads/hos/alt":
		//		EncryptionKey = EncryptionKeyType.HeadOfSecurity;
		//		break;
		//	case "/obj/item/device/radio/headset/heads/rd":
		//		EncryptionKey = EncryptionKeyType.ResearchDirector;
		//		break;
		//	case "/obj/item/device/radio/headset/headset_cargo":
		//		EncryptionKey = EncryptionKeyType.Supply;
		//		break;
		//	case "/obj/item/device/radio/headset/headset_cent":
		//	case "/obj/item/device/radio/headset/headset_cent/alt":
		//		EncryptionKey = EncryptionKeyType.CentComm;
		//		break;
		//	case "/obj/item/device/radio/headset/headset_eng":
		//		EncryptionKey = EncryptionKeyType.Engineering;
		//		break;
		//	case "/obj/item/device/radio/headset/headset_med":
		//		EncryptionKey = EncryptionKeyType.Medical;
		//		break;
		//	case "/obj/item/device/radio/headset/headset_sci":
		//		EncryptionKey = EncryptionKeyType.Science;
		//		break;
		//	case "/obj/item/device/radio/headset/headset_sec":
		//	case "/obj/item/device/radio/headset/headset_sec/alt":
		//		EncryptionKey = EncryptionKeyType.Security;
		//		break;
		//	case "/obj/item/device/radio/headset/headset_srv":
		//		EncryptionKey = EncryptionKeyType.Service;
		//		break;
		//	case "/obj/item/device/radio/headset/syndicate/alt":
		//		EncryptionKey = EncryptionKeyType.Syndicate;
		//		break;
		//	default:
		//		EncryptionKey = EncryptionKeyType.Common;
		//		break;
		//}
	}
}