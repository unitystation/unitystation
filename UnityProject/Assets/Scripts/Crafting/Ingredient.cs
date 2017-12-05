using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Crafting
{
    [System.Serializable]
    public class Ingredient
    {
        public string ingredientName;
        public int amount = 1;

        public Ingredient(string ingredientName, int amount = 1)
        {
            this.ingredientName = ingredientName;
            this.amount = amount;
        }

        public override bool Equals(object obj)
        {
            var other = (Ingredient)obj;
            return ingredientName == other.ingredientName && amount == other.amount;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
