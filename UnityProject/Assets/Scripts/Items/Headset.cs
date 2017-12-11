using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Headset properties
/// </summary>
public class Headset : NetworkBehaviour
{
	[SyncVar]
	public EncryptionKeyType EncryptionKey;

	public void init()
	{
		getEncryptionTypeFromHier();
	}

	public void InsertEncryptionKey(EncryptionKey encryptionKeyComponent)
	{
		if (EncryptionKey != EncryptionKeyType.None)
		{
			UI.UIManager.Chat.AddChatEvent(new ChatEvent("This headset already has an encryption key", ChatChannel.System));
			return;
		}

		EncryptionKey = encryptionKeyComponent.Type;
		GameObject.Destroy(encryptionKeyComponent.gameObject);
	}

	private void getEncryptionTypeFromHier()
	{
		ItemAttributes attr = GetComponent<ItemAttributes>();

		switch(attr.hierarchy)
		{
			case "/obj/item/device/radio/headset":
				EncryptionKey = EncryptionKeyType.Common;
				break;
			case "/obj/item/device/radio/headset/heads/captain":
			case "/obj/item/device/radio/headset/heads/captain/alt":
				EncryptionKey = EncryptionKeyType.Captain;
				break;
			case "/obj/item/device/radio/headset/heads/ce":
				EncryptionKey = EncryptionKeyType.ChiefEngineer;
				break;
			case "/obj/item/device/radio/headset/heads/cmo":
				EncryptionKey = EncryptionKeyType.ChiefMedicalOfficer;
				break;
			case "/obj/item/device/radio/headset/heads/hop":
				EncryptionKey = EncryptionKeyType.HeadOfPersonnel;
				break;
			case "/obj/item/device/radio/headset/heads/hos":
			case "/obj/item/device/radio/headset/heads/hos/alt":
				EncryptionKey = EncryptionKeyType.HeadOfSecurity;
				break;
			case "/obj/item/device/radio/headset/heads/rd":
				EncryptionKey = EncryptionKeyType.ResearchDirector;
				break;
			case "/obj/item/device/radio/headset/headset_cargo":
				EncryptionKey = EncryptionKeyType.Supply;
				break;
			case "/obj/item/device/radio/headset/headset_cent":
			case "/obj/item/device/radio/headset/headset_cent/alt":
				EncryptionKey = EncryptionKeyType.CentComm;
				break;
			case "/obj/item/device/radio/headset/headset_eng":
				EncryptionKey = EncryptionKeyType.Engineering;
				break;
			case "/obj/item/device/radio/headset/headset_med":
				EncryptionKey = EncryptionKeyType.Medical;
				break;
			case "/obj/item/device/radio/headset/headset_sci":
				EncryptionKey = EncryptionKeyType.Science;
				break;
			case "/obj/item/device/radio/headset/headset_sec":
			case "/obj/item/device/radio/headset/headset_sec/alt":
				EncryptionKey = EncryptionKeyType.Security;
				break;
			case "/obj/item/device/radio/headset/headset_srv":
				EncryptionKey = EncryptionKeyType.Service;
				break;
			case "/obj/item/device/radio/headset/syndicate/alt":
				EncryptionKey = EncryptionKeyType.Syndicate;
				break;
			default:
				EncryptionKey = EncryptionKeyType.Common;
				break;
		}
	}
}
