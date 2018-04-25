using UnityEngine;

namespace Tilemaps.Behaviours
{
	public class MatrixTest: MonoBehaviour
	{
		private Matrix matrix;

		private void Start()
		{
			matrix = GetComponent<Matrix>();
		}

		private void Update()
		{
			for (int i = 0; i < 1000; i++)
			{
				matrix.IsPassableAt(Vector3Int.zero);
			}
		}
	}
}