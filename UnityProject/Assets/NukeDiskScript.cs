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
	
	private float maxDistance = 800;
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
			timeCurrent += Time.deltaTime;

			if (timeCurrent > timeCheckDiskLocation)
			{

				if (CheckDiskPos()) { Teleport(); Debug.Log(("Check disk position  =  " + CheckDiskPos().ToString())); }
			
				timeCurrent = 0;
			}
			pick.RefreshUISlotImage();	
	}

	private bool CheckDiskPos()
	{
		if (!MatrixManager.MainStationMatrix.Bounds.Contains(Vector3Int.FloorToInt(gameObject.AssumedWorldPosServer())))
		{
			Debug.Log(("Disk not on station matrix"));
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
		//gameObject.GetComponent<CustomNetTransform>().Stop();
		customNetTrans.SetPosition(position);
		Debug.Log(("Teleport disk to position = " + position.ToString()));
		Debug.Log(("disk to assumed pos  = " + gameObject.AssumedWorldPosServer()));
	}
}
