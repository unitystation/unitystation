using Systems.Ai;
using Mirror;
using UnityEngine;

namespace Objects.Research
{
	public class AiCore : MonoBehaviour
	{
		private AiPlayer linkedPlayer;
		public AiPlayer LinkedPlayer => linkedPlayer;

		[Server]
		public void SetLinkedPlayer(AiPlayer aiPlayer)
		{
			linkedPlayer = aiPlayer;
		}
	}
}
