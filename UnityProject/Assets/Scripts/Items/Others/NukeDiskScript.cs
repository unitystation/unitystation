using System;
using System.Collections;
using System.Collections.Generic;
using Atmospherics;
using UnityEngine;
using Mirror;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;
public class NukeDiskScript : NetworkBehaviour
{
	[SerializeField]
	private float boundRadius = 600;
	private Pickupable pick;
	private CustomNetTransform customNetTrans;
	private RegisterItem registerItem;
	private BoundsInt bound;
	private EscapeShuttle escapeShuttle;

	private float timeCheckDiskLocation = 5.0f;
	private float timeCurrentDisk = 0;

	private float timeCurrentAnimation = 0;

	private bool isInit = false;
	private bool boundsConfigured = false;

	/// <summary>
	/// Pinpointers wont find this.
	/// </summary>
	public bool secondaryNukeDisk;

	/// <summary>
	/// Stops the disk from teleporting if moved off station matrix.
	/// </summary>
	public bool stopAutoTeleport;

	public override void OnStartServer()
	{
		base.OnStartServer();
		Init();
	}

	public override void OnStartClient()
	{
		base.OnStartClient();
		Init();
	}

	void Init()
	{
		if (isInit) return;
		isInit = true;

		customNetTrans = GetComponent<CustomNetTransform>();
		registerItem = GetComponent<RegisterItem>();
		pick = GetComponent<Pickupable>();

		registerItem.WaitForMatrixInit(EnsureInit);
	}

	private void EnsureInit(MatrixInfo matrixInfo)
	{
		bound = MatrixManager.MainStationMatrix.Bounds;
		escapeShuttle = FindObjectOfType<EscapeShuttle>();
		boundsConfigured = true;
	}

	private void OnEnable()
	{
		UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
	}
	void OnDisable()
	{
		UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
	}

	protected virtual void UpdateMe()
	{
		if (!boundsConfigured) return;

		if (isServer)
		{
			timeCurrentDisk += Time.deltaTime;

			if (timeCurrentDisk > timeCheckDiskLocation && !stopAutoTeleport)
			{
				if (DiskLost()) { Teleport();}
				timeCurrentDisk = 0;
			}
		}
		else
		{
			timeCurrentAnimation += Time.deltaTime;
			if (timeCurrentAnimation > 0.1f)
			{
				pick.RefreshUISlotImage();
				timeCurrentAnimation = 0;
			}

		}
	}

	private bool DiskLost()
	{
		if (((gameObject.AssumedWorldPosServer() - MatrixManager.MainStationMatrix.GameObject.AssumedWorldPosServer())
			.magnitude < boundRadius)) return false;

		if (escapeShuttle != null && escapeShuttle.Status != EscapeShuttleStatus.DockedCentcom)
		{
			if (escapeShuttle.MatrixInfo.Bounds.Contains(registerItem.WorldPositionServer))
			{
				return false;
			}
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
		return false;
	}

	private void Teleport()
	{
		Vector3 position = new Vector3(Random.Range(bound.xMin, bound.xMax), Random.Range(bound.yMin,bound.yMax), 0);
		while(MatrixManager.IsSpaceAt(Vector3Int.FloorToInt(position),true) || MatrixManager.IsWallAt(Vector3Int.FloorToInt(position), true))
		{
			position = new Vector3(Random.Range(bound.xMin, bound.xMax), Random.Range(bound.yMin, bound.yMax), 0);
		}

		if (pick?.ItemSlot != null)
		{
			Inventory.ServerDrop(pick.ItemSlot);
			pick.RefreshUISlotImage();
		}
		customNetTrans.SetPosition(position);
	}
}
