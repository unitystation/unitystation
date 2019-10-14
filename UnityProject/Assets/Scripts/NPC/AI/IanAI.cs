using UnityEngine;

/// <summary>
/// Ians magical AI brain!
/// All logic should be server side
/// </summary>
public class IanAI : MobAI
{
	protected override void AIStartServer()
	{
		followingStopped.AddListener(OnFollowingStopped);
		exploringStopped.AddListener(OnExploreStopped);
		fleeingStopped.AddListener(OnFleeStopped);
	}

	public override void LocalChatReceived(ChatEvent chatEvent)
	{
		base.LocalChatReceived(chatEvent);
	}

	void OnFleeStopped()
	{

	}

	void OnExploreStopped()
	{

	}

	void OnFollowingStopped()
	{

	}
}
