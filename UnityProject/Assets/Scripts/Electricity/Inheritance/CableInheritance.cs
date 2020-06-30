using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Mirror;
using UnityEngine.Serialization;

public class CableInheritance : NetworkBehaviour, ICheckedInteractable<PositionalHandApply>
{
	public bool SelfDestruct = false;
	public WiringColor CableType;
	public SpriteSheetAndData CableSprites;
	public Connection WireEndA { get { return wireConnect.InData.WireEndA; } set { wireConnect.InData.WireEndA = value; } }
	public Connection WireEndB { get { return wireConnect.InData.WireEndB; } set { wireConnect.InData.WireEndB = value; } }
	public WireConnect wireConnect;
	public PowerTypeCategory ApplianceType;
	public HashSet<PowerTypeCategory> CanConnectTo;

	[SerializeField]
	[FormerlySerializedAs("Sparks")]
	private ParticleSystem sparksPrefab = null;

	[SerializeField]
	[FormerlySerializedAs("Sparks")]
	private ParticleSystem smokePrefab = null;

	public float MaximumInstantBreakCurrent;
	public float MaximumBreakdownCurrent;
	public float TimeDeforeDestructiveBreakdown;
	public bool CheckDestruction;
	public float DestructionPriority;
	public bool CanOverCurrent = true;

	private bool BeingDestroyed = false;

	private bool CheckOverlap = false;
	public bool IsInGamePlaced = false;

	public bool WillInteract(PositionalHandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;
		if (!Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Wirecutter)) return false;
		if (interaction.TargetObject != gameObject) return false;
		return true;
	}

	public void ServerPerformInteraction(PositionalHandApply interaction)
	{
		//wirecutters can be used to cut this cable
		Vector3Int worldPosInt = interaction.WorldPositionTarget.To2Int().To3Int();
		MatrixInfo matrix = MatrixManager.AtPoint(worldPosInt, true);
		var localPosInt = MatrixManager.WorldToLocalInt(worldPosInt, matrix);
		if (matrix.Matrix != null)
		{
			if (!matrix.Matrix.IsClearUnderfloorConstruction(localPosInt, true))
			{

				return;
			}
		}
		else {

			return;
		}
		wireConnect.DestroyThisPlease();
	}

	public void toDestroy()
	{
		if (wireConnect.RelatedLine != null)
		{
			foreach (var CB in wireConnect.RelatedLine.Covering)
			{
				if (CB == null)
				{
					return;
				}
				CB.Present.GetComponent<CableInheritance>()?.Smoke.Stop();
			}
		}
		GetComponent<CustomNetTransform>().DisappearFromWorldServer();
		SelfDestruct = true;
		//gameObject.GetComponentInChildren<SpriteRenderer>().enabled = false;
		//ElectricalSynchronisation.StructureChange = true;
		PowerUpdateStructureChange();
	}

	public override void OnStartServer()
	{
		base.OnStartServer();
		wireConnect = GetComponent<WireConnect>();
		wireConnect.ControllingCable = this;
		StartCoroutine(WaitForLoad());
		_OnStartServer();
	}

	public virtual void _OnStartServer()
	{
		//wireConnect.ControllingCable = this;
		//StartCoroutine(WaitForLoad());
		//var searchVec = wireConnect.registerTile.LocalPosition;
	}


	public virtual void PowerUpdateStructureChange()
	{
		wireConnect.InData.FlushConnectionAndUp();
		wireConnect.FindPossibleConnections();
		wireConnect.InData.FlushConnectionAndUp();
		if (SelfDestruct)
		{
			wireConnect.registerTile.UnregisterClient();
			wireConnect.registerTile.UnregisterServer();
			if (this != null)
			{
				wireConnect.DestroyThisPlease();
				Despawn.ServerSingle(gameObject);
			}
		}

	}

	public virtual void PowerNetworkUpdate()
	{
		ElectricityFunctions.WorkOutActualNumbers(wireConnect.InData);
		if (CheckOverlap)
		{
			CheckOverlap = false;
			FindOverlapsAndCombine();

			ConvertToTile();

		}
		//if (MaximumInstantBreakCurrent != 0 && CanOverCurrent)
		//{
		//	if (MaximumInstantBreakCurrent < wireConnect.Data.CurrentInWire)
		//	{
		//		QueueForDemolition(this);
		//		return;
		//	}
		//	if (MaximumBreakdownCurrent < wireConnect.Data.CurrentInWire) {
		//		if (CheckDestruction)
		//		{
		//			QueueForDemolition(this);
		//			return;
		//		}
		//		else
		//		{
		//			Smoke.Play();
		//			StartCoroutine(WaitForDemolition());
		//			return;
		//		}
		//	}
		//	if (CheckDestruction)
		//	{
		//		CheckDestruction = false;
		//		Smoke.Stop();
		//	}
		//	Sparks.Stop();
		//}
	}

	public void QueueForDemolition(CableInheritance CableToDestroy)
	{
		var sync = ElectricalManager.Instance.electricalSync;
		DestructionPriority = wireConnect.InData.Data.CurrentInWire * MaximumBreakdownCurrent;
		if (sync.CableToDestroy != null)
		{
			if (DestructionPriority >= sync.CableToDestroy.DestructionPriority)
			{
				sync.CableToDestroy.Smoke.Stop();
				sync.CableToDestroy.Sparks.Stop();
				sync.CableUpdates.Add(sync.CableToDestroy);
				sync.CableToDestroy = this;
			}
			else {
				sync.CableUpdates.Add(this);
			}
		}
		else {
			sync.CableToDestroy = this;
		}
	}


	IEnumerator WaitForDemolition()
	{
		yield return WaitFor.Seconds(TimeDeforeDestructiveBreakdown);
		CheckDestruction = true;
		ElectricalManager.Instance.electricalSync.CableUpdates.Add(this);
	}

	IEnumerator WaitForLoad()
	{
		yield return WaitFor.Seconds(1);
		//Logger.Log("AddElectricalNode");
		if (!IsInGamePlaced)
		{
			ConvertToTile();
		}
	}


	public void ConvertToTile(bool editor = false)
	{
		if (this != null && !BeingDestroyed)
		{
			if (wireConnect.InData.WireEndA != Connection.NA | wireConnect.InData.WireEndB != Connection.NA)
			{
				var searchVec = wireConnect.registerTile.LocalPosition;
				if (wireConnect.SpriteHandler == null)
				{
					BeingDestroyed = true;
					if (editor)
					{
						wireConnect.registerTile.Matrix.EditorAddElectricalNode(searchVec, wireConnect);
					}
					else
					{
						wireConnect.registerTile.Matrix.AddElectricalNode(searchVec, wireConnect);
					}

					//wireConnect.InData = new IntrinsicElectronicData();
					wireConnect.InData.DestroyAuthorised = true;
					wireConnect.InData.DestroyQueueing = true;
					if (editor)
					{
						DestroyImmediate(gameObject);
					}
					else
					{
						Despawn.ServerSingle(gameObject);

					}
					wireConnect.InData.DestroyAuthorised = false;
					wireConnect.InData.DestroyQueueing = false;
					//DestroyImmediate(gameObject); ##d
				}
			}
		}
	}

	/// <summary>
	///     If you have some tray goggles on then set this bool to true to get the right sprite.
	///     I guess you still need to faff about with display layers but that isn't my issue.
	/// </summary>
	public bool TRay;

	public void damEditor()
	{
		SetSprite();
	}



	public void FindOverlapsAndCombine()
	{
		if (WireEndA == Connection.Overlap | WireEndB == Connection.Overlap)
		{
			List<IntrinsicElectronicData> Econns = new List<IntrinsicElectronicData>();

			var IEnumerableEconns = wireConnect.Matrix.GetElectricalConnections(wireConnect.registerTile.LocalPositionServer);
			foreach (var T in IEnumerableEconns)
			{
				Econns.Add(T);
			}
			IEnumerableEconns.Clear();
			ElectricalPool.PooledFPCList.Add(IEnumerableEconns);

			for (int i = 0; i < Econns.Count; i++)
			{
				if (ApplianceType == Econns[i].Categorytype && wireConnect.InData != Econns[i])
				{
					Connection replacementConnection;
					if (Econns[i].WireEndA == Connection.Overlap)
					{
						replacementConnection = Econns[i].WireEndB;
					}
					else if (Econns[i].WireEndB == Connection.Overlap)
					{
						replacementConnection = Econns[i].WireEndA;
					}
					else
					{
						continue;
					}

					if (WireEndA == Connection.Overlap)
					{
						WireEndA = replacementConnection;
					}
					else
					{
						WireEndB = replacementConnection;
					}
					SetDirection(WireEndB, WireEndA, CableType);
					Econns[i].DestroyThisPlease();
					return;
				}
			}
		}
	}

	public void SetDirection(Connection REWireEndA, Connection REWireEndB, WiringColor RECableType = WiringColor.unknown)
	{
		if (REWireEndA == REWireEndB)
		{
			Logger.LogWarningFormat("Wire connection both starts ({0}) and ends ({1}) in the same place!", Category.Electrical, REWireEndA, REWireEndB);
			return;
		}
		if (RECableType != WiringColor.unknown)
		{
			CableType = RECableType;
		}
		WireEndA = REWireEndA;
		WireEndB = REWireEndB;
		SetSprite();
		if (isServer)
		{
			CheckOverlap = true;
			ElectricalManager.Instance.electricalSync.CableUpdates.Add(this);
		}
	}


	[ContextMenu("FindConnections")]
	private void SetSprite()
	{
		SpriteRenderer SR = gameObject.GetComponentInChildren<SpriteRenderer>();
		int spriteIndex = WireDirections.GetSpriteIndex(WireEndA, WireEndB, TRay);
		SR.sprite = CableSprites.Sprites[spriteIndex];
		if (SR.sprite == null)
		{
			Logger.LogError("SetSprite: Couldn't find wire sprite, sprite value didn't return anything!", Category.Electrical);
		}
	}

	private ParticleSystem sparks;
	public ParticleSystem Sparks
	{
		get
		{
			if (!sparks)
				sparks = Instantiate(sparksPrefab, transform);
			return sparks;
		}
	}

	public bool IsSparking()
	{
		return sparks != null && sparks.isPlaying;
	}

	private ParticleSystem smoke;
	public ParticleSystem Smoke
	{
		get
		{
			if (!smoke)
				smoke = Instantiate(smokePrefab, transform);
			return smoke;
		}
	}
}
