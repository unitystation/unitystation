using UnityEngine;

namespace US13.Objects.Engineering.Reactor
{
	public class ReactorChamberRod : MonoBehaviour
	{
		public ReactorGraphiteChamber CurrentlyInstalledIn;

		public RodType rodType;
		public Color color = new Color(0.6666f, 0.6666f, 0.6666f, 1);
		public virtual RodType GetRodType()
		{
			return RodType.Fuel;
		}

		public virtual Color GetUIColour()
		{
			return color;
		}
	}

	public enum RodType
	{
		Fuel,
		Control,
		Starter
	}
}
