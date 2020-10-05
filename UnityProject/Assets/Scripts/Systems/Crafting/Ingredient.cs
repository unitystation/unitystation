using System;


	[Serializable]
	public class Ingredient
	{
		public int requiredAmount = 1;
		public string ingredientName;

		public Ingredient(string ingredientName, int amount = 1)
		{
			this.ingredientName = ingredientName;
			this.requiredAmount = amount;
		}

		public override bool Equals(object obj)
		{
			Ingredient other = (Ingredient) obj;
			return ingredientName == other.ingredientName && requiredAmount == other.requiredAmount;
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
	}
