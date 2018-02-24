using System;

namespace Crafting
{
	[Serializable]
	public class Ingredient
	{
		public int amount = 1;
		public string ingredientName;

		public Ingredient(string ingredientName, int amount = 1)
		{
			this.ingredientName = ingredientName;
			this.amount = amount;
		}

		public override bool Equals(object obj)
		{
			Ingredient other = (Ingredient) obj;
			return ingredientName == other.ingredientName && amount == other.amount;
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
	}
}