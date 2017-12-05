using Sprites;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wiring
{
    [ExecuteInEditMode]
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
        public WiringColor Color = WiringColor.red;
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
            if (DirectionStart == DirectionEnd || DirectionEnd == 0)
            {
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
            Sprite[] Color = SpriteManager.WireSprites[this.Color.ToString()];
            SpriteRenderer SR = this.gameObject.GetComponentInChildren<SpriteRenderer>();
            //the red sprite is spliced differently than the rest for some reason :^(
            int spriteIndex = WireDirections.GetSpriteIndex(spritePath);
            if (this.Color == WiringColor.red)
            {
                spriteIndex *= 2;
                if (this.TRay)
                {
                    spriteIndex++;
                }
            }
            else if (this.TRay)
            {
                spriteIndex += 36;
            }
            SR.sprite = Color[spriteIndex];
            if (SR.sprite == null)
            {
                this.Color = WiringColor.red;
                this.SetDirection(1);
            }
        }
    }
}
