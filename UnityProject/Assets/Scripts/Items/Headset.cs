using System.Collections;
using Communications;
using Items;
using Mirror;
using Systems.Explosions;
using UnityEngine;

/// <summary>
///     Headset properties
/// </summary>
public class Headset : SignalEmitter, IInteractable<HandActivate>, IExaminable, IEmpAble
{
	[SyncVar] public EncryptionKeyType EncryptionKey;
	[SyncVar] public bool LoudSpeakOn = false;
	[SyncVar] public bool isEMPed = false;
	public bool HasLoudSpeak = false;
	public Loudness LoudspeakLevel = Loudness.SCREAMING;

	public void init()
	{
		getEncryptionTypeFromHier();
	}

	public void ServerPerformInteraction(HandActivate interaction)
	{
		if (HasLoudSpeak)
		{
			LoudSpeakOn = !LoudSpeakOn;
			string result = LoudSpeakOn ? "turn on" : "turn off";
			Chat.AddExamineMsg(interaction.Performer, $"You {result} the {gameObject.ExpensiveName()}");
		}
	}

	protected override bool SendSignalLogic()
	{
		if (GameManager.Instance.CommsServers.Count == 0) return false;
		return isEMPed == false;
	}

	/// <summary>
	/// Nothing happens in SS13 when a fail happens so I guess leave it like that
	/// </summary>
	public override void SignalFailed() { }

	public string Examine(Vector3 worldPos = default)
	{
		string status = "";
		if (isEMPed)
		{
			status = $"<color=red> It appears to be disabled. Perhaps it will become active again later?";
		}
		return $"{gameObject.GetComponent<ItemAttributesV2>().InitialDescription}" +status;
	}

	public void OnEmp(int EmpStrength)
	{
		if (isEMPed == false)
		{
			StartCoroutine(Emp(EmpStrength));
		}
	}

	public IEnumerator Emp(int EmpStrength)
	{
		int effectTime = (int)(EmpStrength * 0.75f);
		isEMPed = true;
		Chat.AddExamineMsg(PlayerManager.LocalPlayerScript.gameObject, $"Your {gameObject.ExpensiveName()} suddenly becomes very quiet...");
		yield return WaitFor.Seconds(effectTime);
		isEMPed = false;
		Chat.AddExamineMsg(PlayerManager.LocalPlayerScript.gameObject, $"Your {gameObject.ExpensiveName()} became emmiting buzz and radio messages again.");
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