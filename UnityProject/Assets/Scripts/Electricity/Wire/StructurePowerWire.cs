using System;
using System.Collections.Generic;
using UnityEngine;

public class StructurePowerWire : MonoBehaviour, ICable
{
	public ICable RelatedCable;
	public WiringColor CableType { get; set; }
	public bool IsCable { get; set; }
	/// <summary>
	///     Color of the wire
	/// </summary>
	public WiringColor Color;
	/// <summary>
	///     The ending dir of this wire in a turf, using 4 bits to indicate N S E W - 1 2 4 8
	///     Corners can also be used i.e.: 5 = NE (1 + 4) = 0101
	///     This is the edge of the location where the wire exits the turf
	///     Can be null of knot wires
	/// </summary>
	public int DirectionEnd { get; set; }

	/// <summary>
	///     The starting dir of this wire in a turf, using 4 bits to indicate N S E W - 1 2 4 8
	///     Corners can also be used i.e.: 5 = NE (1 + 4) = 0101
	///     This is the edge of the location where the wire enters the turf
	/// </summary>
	public int DirectionStart { get; set; }

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
		IsCable = false;
		var ListOfICable = this.GetComponents<ICable>();
		foreach (ICable Cable in ListOfICable)
		{
			if (Cable.IsCable)
			{
				//Logger.Log ("Found it", Category.Electrical);
				RelatedCable = Cable;
			}
		}
		if (!(RelatedCable == null))
		{
			Color = RelatedCable.CableType;
			DirectionEnd = RelatedCable.DirectionEnd;
			DirectionStart = RelatedCable.DirectionStart;
			//FIXME this breaks wires that were placed via unity editor:
			// need to address when we allow users to add wires at runtime
			SetDirection(DirectionStart, DirectionEnd);
		}
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
		this.DirectionStart = Math.Min(DirectionStart, DirectionEnd);
		this.DirectionEnd = Math.Max(DirectionStart, DirectionEnd);
		//Logger.Log(DirectionStart.ToString() + " <DirectionStart and DirectionEnd> " + DirectionEnd.ToString(), Category.Electrical);
		SetSprite();
	}

	[ContextMenu("FindConnections")]
	private void SetSprite()
	{
		string spritePath = DirectionStart + (DirectionEnd != 0 ? "_" + DirectionEnd : "");
		Sprite[] Color = SpriteManager.WireSprites[this.Color.ToString()];
		if (Color == null)
		{
			SpriteManager.Instance.InitWireSprites();
			Color = SpriteManager.WireSprites[this.Color.ToString()];
		}
		SpriteRenderer SR = gameObject.GetComponentInChildren<SpriteRenderer>();
		//the red sprite is spliced differently than the rest for some reason :^(
		int spriteIndex = WireDirections.GetSpriteIndex(spritePath);
		if (this.Color == WiringColor.red)
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
			this.Color = WiringColor.red;
			SetDirection(1);
		}
	}
}