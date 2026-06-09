using UnityEngine;
using US13.ScriptableObjects.RP;

namespace US13.Player.EmoteScripts
{
	[CreateAssetMenu(fileName = "DryHeav", menuName = "ScriptableObjects/RP/Emotes/DryHeav")]
	public class DryHeav : GenderedEmote
	{
		[SerializeField] private float stunDuration = 8f;

		public override void Do(GameObject actor)
		{
			base.Do(actor);
			var script = actor.GetComponent<PlayerScript>();
			script.RegisterPlayer.ServerStun(stunDuration);
			script.RegisterPlayer.ServerLayDown();
		}
	}
}