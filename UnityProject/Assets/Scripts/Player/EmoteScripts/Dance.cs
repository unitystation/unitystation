using System.Collections;
using UnityEngine;


[CreateAssetMenu(fileName = "Emote", menuName = "ScriptableObjects/RP/Emotes/Dance")]
public class Dance : EmoteSO
{
	public override void Do(GameObject player)
	{
		//I can't use StartCoroutine in a scriptableobject so someone else find a solution to this.
		dance(player);
	}

	private void dance(GameObject player)
	{
		Directional directional = player.transform.GetComponent<Directional>();
		PlayerMove move = player.transform.GetComponent<PlayerMove>();
		if (move.allowInput && !move.IsBuckled)
		{
			//Yes I know this is stupid but Unity is stupid as well.
			//No, I'm not making a seperate script for this because that's stupid as well.
			directional.FaceDirection(Orientation.Up);
			directional.FaceDirection(Orientation.Left);
			directional.FaceDirection(Orientation.Right);
			directional.FaceDirection(Orientation.Down);
			directional.FaceDirection(Orientation.Up);
			directional.FaceDirection(Orientation.Left);
			directional.FaceDirection(Orientation.Right);
		}
	}
}
