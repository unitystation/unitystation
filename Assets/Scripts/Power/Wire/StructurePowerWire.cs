using Sprites;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Wiring
{
	public class StructurePowerWire : MonoBehaviour
	{
		/// <summary>
		/// The starting dir of this wire in a turf, using 4 bits to indicate N S E W - 1 2 4 8
		/// Corners can also be used i.e.: 5 = NE (1 + 4) = 0101
		/// This is the edge of the location where the wire enters the turf
		/// </summary>
		public int DirectionStart = 2;
		/// <summary>
		/// The ending dir of this wire in a turf, using 4 bits to indicate N S E W - 1 2 4 8
		/// Corners can also be used i.e.: 5 = NE (1 + 4) = 0101
		/// This is the edge of the location where the wire exits the turf
		/// Can be null of knot wires
		/// </summary>
		public int DirectionEnd = 0;
		/// <summary>
		/// Color of the wire
		/// </summary>
		public string Color = "red";
		/// <summary>
		/// If you have some tray goggles on then set this bool to true to get the right sprite.
		/// I guess you still need to faff about with display layers but that isn't my issue.
		/// </summary>
		public bool TRay = false;
		/// <summary>
		/// Go to the Start method to add to this.
		/// Push the sprite things with a string color as key so you can change it easily or whatever.
		/// </summary>
		protected static Dictionary<string, Sprite[]> ColorToSpriteArray;
		// Use this for initialization
		void Start()
		{
			this.SetDirection(this.DirectionStart, this.DirectionEnd);
		}
		public void SetDirection(int DirectionStart)
		{
			this.DirectionStart = DirectionStart;
			this.DirectionEnd = 0;
			SetSprite();
		}
		public void SetDirection(int DirectionStart, int DirectionEnd)
		{
			//This ensures that End is just null when they are the same
			if (DirectionStart == DirectionEnd || DirectionEnd == 0) {
				SetDirection(DirectionStart);
				return;
			}
			//This ensures that the DirectionStart is always the lower one after constructing it.
			//It solves some complexity issues with the sprite's path
			//Casting here is to solve nullable somehow not noticing my nullcheck earlier
			this.DirectionStart = System.Math.Min(DirectionStart, DirectionEnd);
			this.DirectionEnd = System.Math.Max(DirectionStart, DirectionEnd);
			SetSprite();
		}
		private void SetSprite()
		{
			string spritePath = this.DirectionStart + (this.DirectionEnd != 0 ? "_" + this.DirectionEnd : "");
			Sprite[] Color = SpriteManager.WireSprites[this.Color];
			SpriteRenderer SR = this.gameObject.GetComponentInChildren<SpriteRenderer>();
			//the red sprite is spliced differently than the rest for some reason :^(
			int spriteIndex = GetSpriteIndex(spritePath);
			if (this.Color.Equals("red")) {
				spriteIndex *= 2;
				if (this.TRay) {
					spriteIndex++;
				}
			} else if (this.TRay) {
				spriteIndex += 36;
			}
			SR.sprite = Color[spriteIndex];
			if (SR.sprite == null) {
				this.Color = "red";
				this.SetDirection(1);
			}

		}

		protected static Dictionary<string, int> LogicToIndexMap;
		// This thing will fucking toss a null pointer if you use it wrong so don't set it to public thinking you're smart
		protected static int GetSpriteIndex(string logic)
		{
			if (StructurePowerWire.LogicToIndexMap == null) {
				StructurePowerWire.LogicToIndexMap = new Dictionary<string, int>();

				StructurePowerWire.LogicToIndexMap.Add("1", 0);
				StructurePowerWire.LogicToIndexMap.Add("2", 1);
				StructurePowerWire.LogicToIndexMap.Add("4", 2);
				StructurePowerWire.LogicToIndexMap.Add("5", 3);
				StructurePowerWire.LogicToIndexMap.Add("6", 4);
				StructurePowerWire.LogicToIndexMap.Add("8", 5);
				StructurePowerWire.LogicToIndexMap.Add("9", 6);
				StructurePowerWire.LogicToIndexMap.Add("10", 7);
				StructurePowerWire.LogicToIndexMap.Add("1_2", 8);
				StructurePowerWire.LogicToIndexMap.Add("1_4", 9);
				StructurePowerWire.LogicToIndexMap.Add("1_5", 10);
				StructurePowerWire.LogicToIndexMap.Add("1_6", 11);
				StructurePowerWire.LogicToIndexMap.Add("1_8", 12);
				StructurePowerWire.LogicToIndexMap.Add("1_9", 13);
				StructurePowerWire.LogicToIndexMap.Add("1_10", 14);
				StructurePowerWire.LogicToIndexMap.Add("2_4", 15);
				StructurePowerWire.LogicToIndexMap.Add("2_5", 16);
				StructurePowerWire.LogicToIndexMap.Add("2_6", 17);
				StructurePowerWire.LogicToIndexMap.Add("2_8", 18);
				StructurePowerWire.LogicToIndexMap.Add("2_9", 19);
				StructurePowerWire.LogicToIndexMap.Add("2_10", 20);
				StructurePowerWire.LogicToIndexMap.Add("4_5", 21);
				StructurePowerWire.LogicToIndexMap.Add("4_6", 22);
				StructurePowerWire.LogicToIndexMap.Add("4_8", 23);
				StructurePowerWire.LogicToIndexMap.Add("4_9", 24);
				StructurePowerWire.LogicToIndexMap.Add("4_10", 25);
				StructurePowerWire.LogicToIndexMap.Add("5_6", 26);
				StructurePowerWire.LogicToIndexMap.Add("5_8", 27);
				StructurePowerWire.LogicToIndexMap.Add("5_9", 28);
				StructurePowerWire.LogicToIndexMap.Add("5_10", 29);
				StructurePowerWire.LogicToIndexMap.Add("6_8", 30);
				StructurePowerWire.LogicToIndexMap.Add("6_9", 31);
				StructurePowerWire.LogicToIndexMap.Add("6_10", 32);
				StructurePowerWire.LogicToIndexMap.Add("8_9", 33);
				StructurePowerWire.LogicToIndexMap.Add("8_10", 34);
				StructurePowerWire.LogicToIndexMap.Add("9_10", 35);
			}
			return StructurePowerWire.LogicToIndexMap[logic];
		}
	}
}
