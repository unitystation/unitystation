using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Mirror;

public class CableInheritance : NetworkBehaviour, ICheckedInteractable<PositionalHandApply>, IDeviceControl
{
	public bool SelfDestruct = false;
	public WiringColor CableType;
	public SpriteSheetAndData CableSprites;
	public Connection WireEndA { get { return wireConnect.WireEndA; } set { wireConnect.WireEndA = value; } }
	public Connection WireEndB { get { return wireConnect.WireEndB; } set { wireConnect.WireEndB = value; } }
	public WireConnect wireConnect;
	public PowerTypeCategory ApplianceType;
	public HashSet<PowerTypeCategory> CanConnectTo;

	public ParticleSystem Sparks;
	public ParticleSystem Smoke;

	public float MaximumInstantBreakCurrent;
	public float MaximumBreakdownCurrent;
	public float TimeDeforeDestructiveBreakdown;
	public bool CheckDestruction;
	public float DestructionPriority;
	public bool CanOverCurrent = true;


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

		toDestroy();
	}

	public void PotentialDestroyed()
	{
		if (SelfDestruct)
		{
			//Then you can destroy
		}
	}

	public void toDestroy()
	{
		if (wireConnect.RelatedLine != null)
		{
			foreach ( var CB in wireConnect.RelatedLine.Covering )
			{
				if ( CB == null )
				{
					return;
				}
				CB.gameObject.GetComponent<CableInheritance>()?.Smoke.Stop();
			}
		}
		GetComponent<CustomNetTransform>().DisappearFromWorldServer();
		SelfDestruct = true;
		//gameObject.GetComponentInChildren<SpriteRenderer>().enabled = false;
		//ElectricalSynchronisation.StructureChange = true;
		ElectricalSynchronisation.NUCableStructureChange.Add(this);
	}

	void Awake()
	{
		wireConnect = GetComponent<WireConnect>();
		wireConnect.ControllingCable = this;
		wireConnect.InData.ElectricityOverride = true;
	}

	public override void OnStartServer()
	{
		base.OnStartServer();
		_OnStartServer();
	}
	public virtual void _OnStartServer()
	{
	}
	public virtual void PowerUpdateStructureChange()
	{
		wireConnect.FlushConnectionAndUp();
		wireConnect.FindPossibleConnections();
		wireConnect.FlushConnectionAndUp();
		if (SelfDestruct) {
			wireConnect.registerTile.UnregisterClient();
			wireConnect.registerTile.UnregisterServer();
			if (this != null)
			{
				Despawn.ServerSingle(gameObject);
			}
		}

	}

	public virtual void PowerNetworkUpdate()
	{
		ElectricityFunctions.WorkOutActualNumbers(wireConnect);
		if (MaximumInstantBreakCurrent != 0 && CanOverCurrent)
		{
			if (MaximumInstantBreakCurrent < wireConnect.Data.CurrentInWire)
			{
				QueueForDemolition(this);
				return;
			}
			if (MaximumBreakdownCurrent < wireConnect.Data.CurrentInWire) {
				if (CheckDestruction)
				{
					if (wireConnect.RelatedLine != null)
					{
						foreach (var CB in wireConnect.RelatedLine.Covering)
							CB.gameObject.GetComponent<CableInheritance>()?.Smoke.Stop();
					}
					QueueForDemolition(this);
					return;
				}
				else
				{
					if (wireConnect.RelatedLine != null) {
						foreach (var CB in wireConnect.RelatedLine.Covering)
							CB.gameObject.GetComponent<CableInheritance>()?.Smoke.Play();
					}
					Smoke.Play();
					StartCoroutine(WaitForDemolition());
					return;
				}
			}
			if (CheckDestruction)
			{
				CheckDestruction = false;
				if (wireConnect.RelatedLine != null)
				{
					foreach (var CB in wireConnect.RelatedLine.Covering)
						CB.gameObject.GetComponent<CableInheritance>()?.Smoke.Stop();
				}
				Smoke.Stop();
			}

			if ( Sparks )
			{
				Sparks.Stop();
			}
		}
	}

	public void QueueForDemolition(CableInheritance CableToDestroy)
	{
		Sparks.Play();
		DestructionPriority = wireConnect.Data.CurrentInWire * MaximumBreakdownCurrent;
		if (ElectricalSynchronisation.CableToDestroy != null)
		{
			if (DestructionPriority > ElectricalSynchronisation.CableToDestroy.DestructionPriority)
			{
				ElectricalSynchronisation.CableUpdates.Add(ElectricalSynchronisation.CableToDestroy);
				ElectricalSynchronisation.CableToDestroy = this;
			}
			else {
				ElectricalSynchronisation.CableUpdates.Add(this);
			}
		}
		else {
			ElectricalSynchronisation.CableToDestroy = this;
		}
	}


	IEnumerator WaitForDemolition()
	{
		yield return WaitFor.Seconds(TimeDeforeDestructiveBreakdown);
		CheckDestruction = true;
		ElectricalSynchronisation.CableUpdates.Add(this);
	}

	//FIXME: Objects at runtime do not get destroyed. Instead they are returned back to pool
	//FIXME: that also renderers IDevice useless. Please reassess
	public void OnDestroy()
	{
		SelfDestruct = true;
		//Making Invisible
	}
	public void TurnOffCleanup()
	{
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
	// Use this for initialization
	private void Start()
	{
		ElectricalSynchronisation.NUCableStructureChange.Add(this);
		SetDirection(WireEndB, WireEndA, CableType);
	}

	public void FindOverlapsAndCombine()
	{
		if (WireEndA == Connection.Overlap | WireEndB == Connection.Overlap)
		{
			bool isA;
			if (WireEndA == Connection.Overlap)
			{
				isA = true;
			}
			else {
				isA = false;
			}
			List<ElectricalOIinheritance> Econns = new List<ElectricalOIinheritance>();
			var IEnumerableEconns = wireConnect.Matrix.GetElectricalConnections(wireConnect.registerTile.LocalPositionServer);
			foreach (var T in IEnumerableEconns) {
				Econns.Add(T);
			}
			int i = 0;
			if (Econns != null)
			{
				while (!(i >= Econns.Count)){
					if (ApplianceType == Econns[i].InData.Categorytype)
					{
						if (wireConnect != Econns[i])
						{
							if (Econns[i].WireEndA == Connection.Overlap)
							{
								if (isA)
								{
									WireEndA = Econns[i].WireEndB;
								}
								else {
									WireEndB = Econns[i].WireEndB;
								}
								SetDirection(WireEndB, WireEndA, CableType);
								ElectricalCableMessage.Send(gameObject, WireEndA, WireEndB, CableType);
								Econns[i].gameObject.GetComponent<CableInheritance>().toDestroy();
							}
							else if (Econns[i].WireEndB == Connection.Overlap)
							{
								if (isA)
								{
									WireEndA = Econns[i].WireEndA;
								}
								else {
									WireEndB = Econns[i].WireEndA;
								}
								SetDirection(WireEndB, WireEndA, CableType);
								ElectricalCableMessage.Send(gameObject, WireEndA, WireEndB, CableType);
								Econns[i].gameObject.GetComponent<CableInheritance>().toDestroy();
							}
						}
					}
					i++;
				}
			}
		}
	}

	public void SetDirection(Connection REWireEndA, Connection REWireEndB, WiringColor RECableType = WiringColor.unknown)
	{
		if (REWireEndA == REWireEndB) {
			Logger.LogWarningFormat("Wire connection both starts ({0}) and ends ({1}) in the same place!", Category.Electrical, REWireEndA, REWireEndB);
			return;
		}
		if (!(RECableType == WiringColor.unknown))
		{
			CableType = RECableType;
		}
		WireEndA = REWireEndA;
		WireEndB = REWireEndB;
		//Logger.Log(WireEndB.ToString() + " <WireEndB and WireEndA> " + WireEndA.ToString(), Category.Electrical);
		SetSprite();
		if (isServer) {
			FindOverlapsAndCombine();
		}
	}


	[ContextMenu("FindConnections")]
	private void SetSprite()
	{
		SpriteRenderer SR = gameObject.GetComponentInChildren<SpriteRenderer>();
		//the red sprite is spliced differently than the rest for some reason :^(
		string Compound;
		if (WireEndA < WireEndB)
		{
			Compound = WireEndA + "_" + WireEndB;
		}
		else {
			Compound = WireEndB + "_" + WireEndA;
		}
		int spriteIndex = WireDirections.GetSpriteIndex(Compound);
		if (TRay)
		{
			spriteIndex += 36;
		}

		SR.sprite = CableSprites.Sprites[spriteIndex];
		if (SR.sprite == null)
		{
			Logger.LogError("SetSprite: Couldn't find wire sprite, sprite value didn't return anything!", Category.Electrical);
		}
	}
}
