using System;
using System.Collections;
using System.Collections.Generic;
using Atmospherics;
using UnityEngine;
using Mirror;
using UnityEngine.Events;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;
public class NukeDiskScript : NetworkBehaviour
{
	private Pickupable pick;
	private CustomNetTransform customNetTrans;
	private RegisterItem registerItem;
	private BoundsInt bound;
	private EscapeShuttle escapeShuttle;

	private float timeCheckDiskLocation = 1.0f;
	private float timeCurrent = 0;
	
	// Start is called before the first frame update
	private void Awake()
	{
		customNetTrans = GetComponent<CustomNetTransform>();
		registerItem = GetComponent<RegisterItem>();
		bound = MatrixManager.MainStationMatrix.Bounds;
		escapeShuttle = FindObjectOfType<EscapeShuttle>();
		pick = GetComponent<Pickupable>();
	}

	public override void OnStartServer()
	{
		UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
	}
	void OnDisable()
	{
		if (isServer)
		{
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
		}
	}

	protected virtual void UpdateMe()
	{
		if (CustomNetworkManager.Instance._isServer)
		{
			timeCurrent += Time.deltaTime;

			if (timeCurrent > timeCheckDiskLocation)
			{

				if (DiskLost()) { Teleport();}

				timeCurrent = 0;
			}
		}
			
	}

	private bool DiskLost()
	{
		if (!bound.Contains(Vector3Int.FloorToInt(gameObject.AssumedWorldPosServer())))
		{
			if (escapeShuttle != null && escapeShuttle.Status != ShuttleStatus.DockedCentcom)
			{
				if (escapeShuttle.MatrixInfo.Bounds.Contains(registerItem.WorldPositionServer))
				{
					return false;
				}
				return true;
			}
			else
			{
				ItemSlot slot = pick.ItemSlot;
				if (slot == null)
				{
					return true;
				}
				RegisterPlayer player = slot.Player;
				if (player == null)
				{
					return true;
				}
				if (player.GetComponent<PlayerHealth>().IsDead)
				{
					return true;
				}
				var checkPlayer = PlayerList.Instance.Get(player.gameObject);
				if(checkPlayer == null)
				{
					return true;
				}
				if(!PlayerList.Instance.AntagPlayers.Contains(checkPlayer))
				{
					return true;
				}

			}
		}
		return false;
	}

	private void Teleport()
	{
		Vector3 position = new Vector3(Random.Range(bound.xMin, bound.xMax), Random.Range(bound.yMin,bound.yMax), 0);
		while(MatrixManager.IsSpaceAt(Vector3Int.FloorToInt(position),true) || MatrixManager.IsWallAt(Vector3Int.FloorToInt(position), true))
		{
			position = new Vector3(Random.Range(bound.xMin, bound.xMax), Random.Range(bound.yMin, bound.yMax), 0);
		}
		customNetTrans.SetPosition(position);
	}
}
