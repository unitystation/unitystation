using UnityEngine;

namespace US13.Shuttles
{
	public class MatrixMove : MonoBehaviour
	{

		private NetworkedMatrixMove networkedMatrixMove;

		public NetworkedMatrixMove NetworkedMatrixMove
		{
			get
			{
				if (networkedMatrixMove == null)
				{
					if (transform.childCount > 1)
					{
						networkedMatrixMove = transform.GetChild(1).GetComponent<NetworkedMatrixMove>();
					}
				}
				return networkedMatrixMove;
			}
		}
	}
}
