using System;
using System.Collections;
using System.Collections.Generic;
using AddressableReferences;
using UnityEngine;
using Random = UnityEngine.Random;

public class GeigerCounter : MonoBehaviour, IInteractable<HandActivate>, IServerInventoryMove
{
	public List<AddressableAudioSource> Low = new List<AddressableAudioSource>();
	public List<AddressableAudioSource> Mid = new List<AddressableAudioSource>();
	public List<AddressableAudioSource> High = new List<AddressableAudioSource>();
	public List<AddressableAudioSource> Extreme = new List<AddressableAudioSource>();

	System.Random RNG = new System.Random();
	private RegisterPlayer registerPlayer;

	private RegisterItem registerItem = null;

	private void OnEnable()
	{
		if (CustomNetworkManager.IsServer == false) return;

		UpdateManager.Add(CycleUpdate, 1f);
	}

	private void OnDisable()
	{
		if (CustomNetworkManager.IsServer == false) return;

		UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, CycleUpdate);
	}

	public void Awake()
	{
		registerItem = this.GetComponent<RegisterItem>();
	}

	public void CycleUpdate()
	{
		//TODO optimise by having a loop on the client side that picks the random noises and only get updates when it changes intensity or turns off
		//TODO should integrate this into register item
		MetaDataNode node = null;
		if (registerPlayer == null)
		{
			node = registerItem.Matrix.GetMetaDataNode(registerItem.LocalPosition);
		}
		else
		{
			node = registerItem.Matrix.GetMetaDataNode(registerPlayer.LocalPosition);
		}

		if (node  == null) return;
		if (node.RadiationNode.RadiationLevel > 1000)
		{
			PlaySound(Extreme[RNG.Next(0,3)]);
		}
		else if (node.RadiationNode.RadiationLevel > 500)
		{
			PlaySound(High[RNG.Next(0,3)]);
		}
		else if (node.RadiationNode.RadiationLevel > 100)
		{
			PlaySound(Mid[RNG.Next(0,3)]);
		}
		else if (node.RadiationNode.RadiationLevel > 20)
		{
			PlaySound(Low[RNG.Next(0,3)]);
		}
	}

	private void PlaySound(AddressableAudioSource sound)
	{
		SoundManager.PlayNetworkedAtPos(sound, registerItem.WorldPositionServer, sourceObj: gameObject);
	}

	public void ServerPerformInteraction(HandActivate interaction)
	{
		Vector3Int worldPosInt = interaction.Performer.GetComponent<PlayerScript>().registerTile.WorldPosition;
		MatrixInfo matrixinfo = MatrixManager.AtPoint(worldPosInt, true);
		var localPosInt = MatrixManager.WorldToLocalInt(worldPosInt, matrixinfo);
		var matrix = interaction.Performer.GetComponentInParent<Matrix>();
		var MetaDataNode = matrix.GetMetaDataNode(localPosInt);
		Chat.AddExamineMsgFromServer(interaction.Performer,
			" The Geiger counter reads " + MetaDataNode.RadiationNode.RadiationLevel);
	}

	public void OnInventoryMoveServer(InventoryMove info)
	{
		if (info.ToPlayer == null)
		{
			registerPlayer = null;
		}
		else
		{
			registerPlayer = info.ToPlayer;
		}
	}

}
