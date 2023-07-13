using ScriptableObjects.RP;
using UnityEngine;

namespace Player.EmoteScripts
{
	[CreateAssetMenu(fileName = "DryHeav", menuName = "ScriptableObjects/RP/Emotes/DryHeav")]
	public class DryHeav : GenderedEmote
	{
		[SerializeField] private float stunDuration = 8f;

		public override void Do(GameObject player)
		{
			base.Do(player);
			var script = player.GetComponent<PlayerScript>();
			script.RegisterPlayer.ServerStun(stunDuration);
			script.RegisterPlayer.ServerLayDown();
		}
	}
}