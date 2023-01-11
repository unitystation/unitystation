using System;
using System.Collections;
using System.Collections.Generic;
using AddressableReferences;
using UnityEngine;
using Random = UnityEngine.Random;

public class GeigerCounter : MonoBehaviour, IInteractable<HandActivate>, IServerSpawn
{
	[SerializeField]
	private List<AddressableAudioSource> lowSounds = new List<AddressableAudioSource>();
	[SerializeField]
	private List<AddressableAudioSource> medSounds = new List<AddressableAudioSource>();
	[SerializeField]
	private List<AddressableAudioSource> highSounds = new List<AddressableAudioSource>();
	[SerializeField]
	private List<AddressableAudioSource> extremeSounds = new List<AddressableAudioSource>();

	System.Random RNG = new System.Random();

	public void OnSpawnServer(SpawnInfo info)
	{
		UpdateManager.Add(CycleUpdate, 1f);
	}

	public void OnDisable()
	{
		UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, CycleUpdate);
	}

	public void CycleUpdate()
	{
		//TODO optimise by having a loop on the client side that picks the random noises and only get updates when it changes intensity or turns off
		//TODO should integrate this into register item
		MetaDataNode node = null;
		node = MatrixManager.GetMetaDataAt(gameObject.AssumedWorldPosServer().RoundToInt());
		if (node  == null) return;
		if (node.RadiationNode.RadiationLevel > 1000)
		{
			PlaySound(extremeSounds.GetRandom());
		}
		else if (node.RadiationNode.RadiationLevel > 500)
		{
			PlaySound(highSounds.GetRandom());
		}
		else if (node.RadiationNode.RadiationLevel > 100)
		{
			PlaySound(medSounds.GetRandom());
		}
		else if (node.RadiationNode.RadiationLevel > 20)
		{
			PlaySound(lowSounds.GetRandom());
		}
	}

	private void PlaySound(AddressableAudioSource sound)
	{
		SoundManager.PlayNetworkedAtPos(sound, gameObject.AssumedWorldPosServer(), sourceObj: gameObject);
	}

	public void ServerPerformInteraction(HandActivate interaction)
	{

		var metaDataNode = MatrixManager.GetMetaDataAt(gameObject.AssumedWorldPosServer().RoundToInt());
		Chat.AddExamineMsgFromServer(interaction.Performer,
			" The Geiger counter reads " + metaDataNode.RadiationNode.RadiationLevel);
	}


}
