using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Only works serverside.
/// This will trigger ambience for players entering or leaving
/// its trigger area. Drag and drop the prefab to start setting up
/// an ambient controlled zone
/// </summary>
public class AmbientSoundArea : MonoBehaviour
{
	private List<GameObject> enteredPlayers = new List<GameObject>();
	[SerializeField] private List<string> enteringSoundTrack = new List<string>();
	[SerializeField] private List<string> leavingSoundTrack = new List<string>();

	public void OnTriggerEnter2D(Collider2D coll)
	{
		if (coll.gameObject.layer == LayerMask.NameToLayer("Players"))
		{
			ValidatePlayer(coll.gameObject, true);
		}
	}

	public void OnTriggerExit2D(Collider2D coll)
	{
		if (coll.gameObject.layer == LayerMask.NameToLayer("Players"))
		{
			ValidatePlayer(coll.gameObject, false);
		}
	}

	IEnumerator Start()
	{
		yield return WaitFor.EndOfFrame;
		if (!CustomNetworkManager.Instance._isServer)
		{
			Destroy(gameObject);
		}
	}

	void ValidatePlayer(GameObject player, bool isEntering)
	{
		if (player == null) return;

		if (isEntering)
		{
			if (enteredPlayers.Contains(player))
			{
				return;
			}

			enteredPlayers.Add(player);
			PlayTrack(player, enteringSoundTrack);
		}
		else
		{
			if (enteredPlayers.Contains(player))
			{
				PlayTrack(player, leavingSoundTrack);
				enteredPlayers.Remove(player);
			}
		}
	}

	void PlayTrack(GameObject player, List<string> possibleTracks)
	{
		if (possibleTracks == null || possibleTracks.Count == 0) return;

		if (possibleTracks.Count == 1)
		{
			PlayAmbientTrack.Send(player, possibleTracks[0]);
			return;
		}

		var randTrack = Random.Range(0, possibleTracks.Count);
		PlayAmbientTrack.Send(player, possibleTracks[randTrack]);
	}
}
