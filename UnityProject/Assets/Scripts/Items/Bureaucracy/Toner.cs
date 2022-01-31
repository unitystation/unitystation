using Mirror;
using UnityEngine;

namespace Items.Bureaucracy
{
	public class Toner : MonoBehaviour, IExaminable
	{
		private int inkLevel;

		[SerializeField] private int maxInkLevel;

		private void Awake()
		{
			inkLevel = maxInkLevel;
		}

		public bool CheckInkLevel()
		{
			if (inkLevel > 0) return true;
			return false;
		}

		[Server]
		public void SpendInk(int amount = 1)
		{
			if (amount <= 0) return;

			inkLevel = Mathf.Max(0, inkLevel - amount);
		}

		public string InkLevel()
		{
			return $"Toner ink level: {inkLevel}/{maxInkLevel}.\n";
		}

		public string Examine(Vector3 worldPos)
		{
			return InkLevel();
		}
	}
}
