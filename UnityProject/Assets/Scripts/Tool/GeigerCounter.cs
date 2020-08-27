using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class GeigerCounter : MonoBehaviour, IInteractable<HandActivate>, IServerInventoryMove
{
	private Dictionary<GeigerCounter.Level, List<string>> Noise = new Dictionary<Level, List<string>>()
	{
		{ Level.Low, new List<string>(){ "low1", "low2", "low3", "low4" } },
		{ Level.Mid, new List<string>(){ "med1", "med2", "med3", "med4" } },
		{ Level.High, new List<string>(){ "high1", "high2", "high3", "high4" } },
		{ Level.Extreme, new List<string>(){ "ext1", "ext2", "ext3", "ext4" } },

	};
	System.Random RNG = new System.Random();
	private RegisterPlayer registerPlayer;
	private enum Level
	{
		Low,
		Mid,
		High,
		Extreme
	}

	private RegisterItem registerItem = null;
	private void OnEnable()
	{
		UpdateManager.Add(CycleUpdate, 1f);
	}

	private void OnDisable()
	{
		UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, CycleUpdate);
	}

	public void Awake()
	{
		registerItem = this.GetComponent<RegisterItem>();
	}

	public void CycleUpdate()
	{
		if(!CustomNetworkManager.IsServer) return;
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
			SoundManager.PlayNetworkedAtPos(Noise[Level.Extreme][RNG.Next(0,3)], registerItem.WorldPositionServer, sourceObj: gameObject);
		}
		else if (node.RadiationNode.RadiationLevel > 500)
		{
			SoundManager.PlayNetworkedAtPos(Noise[Level.High][RNG.Next(0,3)], registerItem.WorldPositionServer, sourceObj: gameObject);
		}
		else if (node.RadiationNode.RadiationLevel > 100)
		{
			SoundManager.PlayNetworkedAtPos(Noise[Level.Mid][RNG.Next(0,3)], registerItem.WorldPositionServer, sourceObj: gameObject);
		}
		else if (node.RadiationNode.RadiationLevel > 20)
		{
			SoundManager.PlayNetworkedAtPos(Noise[Level.Low][RNG.Next(0,3)], registerItem.WorldPositionServer, sourceObj: gameObject);
		}
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
