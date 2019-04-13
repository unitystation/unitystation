using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class CableInheritance : InputTrigger, IDeviceControl
{
	public bool SelfDestruct = false;
	public WiringColor CableType;
	public int DirectionEnd { get { return wireConnect.DirectionEnd; } set { wireConnect.DirectionEnd = value; } }
	public int DirectionStart { get { return wireConnect.DirectionStart; } set { wireConnect.DirectionStart = value; } }
	public WireConnect wireConnect;
	public PowerTypeCategory ApplianceType;
	public HashSet<PowerTypeCategory> CanConnectTo;

	public override bool Interact(GameObject originator, Vector3 position, string hand)// yeah, If anyone works out how  to do it
	{
		Logger.Log("HEYEYEYYEYEYE");
		if (!CanUse(originator, hand, position, false))
		{
			return false;
		}
		if (!isServer)
		{
			InteractMessage.Send(gameObject, hand);
		}
		else {
			var slot = InventoryManager.GetSlotFromOriginatorHand(originator, hand);
			var Wirecutter = slot.Item?.GetComponentInChildren<WirecutterTrigger>();
			if (Wirecutter != null)
			{
				//ElectricalSynchronisation.StructureChange = true;
				wireConnect.registerTile.Unregister();
			}
		}
		return true;
	}
	public void PotentialDestroyed()
	{
		if (SelfDestruct)
		{
			//Then you can destroy
		}
	}
	[ContextMethod("Destroy cable", "x")]
	public void Destroy()
	{
		Logger.Log("1");
		gameObject.GetComponentInChildren<SpriteRenderer>().enabled = false;
		ElectricalSynchronisation.StructureChange = true;
		ElectricalSynchronisation.NUCableStructureChange.Add(this);
	}

	void Awake()
	{
		wireConnect = GetComponent<WireConnect>();
	}

	public override void OnStartServer()
	{
		base.OnStartServer();
		_OnStartServer();
		//wireConnect.InData.ControllingDevice = this;
	}
	public virtual void _OnStartServer()
	{
	}
	public virtual void PowerUpdateStructureChange()
	{
		Logger.Log("2");
		wireConnect.FlushConnectionAndUp();
		wireConnect.registerTile.Unregister();
		if (gameObject != null) { 
			PoolManager.PoolNetworkDestroy(gameObject);
		}

	}
	//FIXME: Objects at runtime do not get destroyed. Instead they are returned back to pool
	//FIXME: that also renderers IDevice useless. Please reassess
	public void OnDestroy()
	{
		//		ElectricalSynchronisation.StructureChangeReact = true;
		//		ElectricalSynchronisation.ResistanceChange = true;
		//		ElectricalSynchronisation.CurrentChange = true;
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

	public void damEditor(int DirectionStart, int DirectionEnd, WiringColor ct)
	{
		CableType = ct;
		//This ensures that End is just null when they are the same
		if (DirectionStart == DirectionEnd || DirectionEnd == 0)
		{
			SetDirection(DirectionStart);
			return;
		}
		//This ensures that the DirectionStart is always the lower one after constructing it.
		//It solves some complexity issues with the sprite's path
		//Casting here is to solve nullable somehow not noticing my nullcheck earlier
		this.DirectionStart = Math.Min(DirectionStart, DirectionEnd);
		this.DirectionEnd = Math.Max(DirectionStart, DirectionEnd);
		//Logger.Log(DirectionStart.ToString() + " <DirectionStart and DirectionEnd> " + DirectionEnd.ToString(), Category.Electrical);
		SetSprite();
	}
	// Use this for initialization
	private void Start()
	{
		//FIXME this breaks wires that were placed via unity editor:
		// need to address when we allow users to add wires at runtime
		SetDirection(DirectionStart, DirectionEnd);
	}

	public void SetDirection(int DirectionStart)
	{
		this.DirectionStart = DirectionStart;
		DirectionEnd = 0;
		SetSprite();
	}

	public void SetDirection(int DirectionStart, int DirectionEnd)
	{
		//This ensures that End is just null when they are the same
		if (DirectionStart == DirectionEnd || DirectionEnd == 0)
		{
			SetDirection(DirectionStart);
			return;
		}
		//This ensures that the DirectionStart is always the lower one after constructing it.
		//It solves some complexity issues with the sprite's path
		//Casting here is to solve nullable somehow not noticing my nullcheck earlier
		DirectionStart = Math.Min(DirectionStart, DirectionEnd);
		DirectionEnd = Math.Max(DirectionStart, DirectionEnd);
		//Logger.Log(DirectionStart.ToString() + " <DirectionStart and DirectionEnd> " + DirectionEnd.ToString(), Category.Electrical);
		SetSprite();
	}


	[ContextMenu("FindConnections")]
	private void SetSprite()
	{
		string spritePath = DirectionStart + (DirectionEnd != 0 ? "_" + DirectionEnd : "");
		Sprite[] Color = SpriteManager.WireSprites[CableType.ToString()];
		if (Color == null)
		{
			SpriteManager.Instance.InitWireSprites();
			Color = SpriteManager.WireSprites[CableType.ToString()];
		}
		SpriteRenderer SR = gameObject.GetComponentInChildren<SpriteRenderer>();
		//the red sprite is spliced differently than the rest for some reason :^(
		int spriteIndex = WireDirections.GetSpriteIndex(spritePath);
		if (CableType == WiringColor.red)
		{
			spriteIndex *= 2;
			if (TRay)
			{
				spriteIndex++;
			}
		}
		else if (TRay)
		{
			spriteIndex += 36;
		}

		SR.sprite = Color[spriteIndex];
		if (SR.sprite == null)
		{
			CableType = WiringColor.red;
			SetDirection(1);
		}
	}
}
