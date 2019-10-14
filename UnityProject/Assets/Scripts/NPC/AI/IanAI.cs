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
		var sanitize = chatEvent.message.ToLower();
		Debug.Log(sanitize);
		if (sanitize.Contains("ian come"))
		{
			FollowTarget(PlayerList.Instance.Get(chatEvent.speaker, false).GameObject.transform);
		}

		if (sanitize.Contains("ian stay"))
		{
			ResetBehaviours();
		}

		if (sanitize.Contains("ian explore"))
		{
			BeginExploring();
		}

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
