using UnityEngine;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// AI brain for mice
/// used to get hunted by Runtime and squeak
/// </summary>
public class MouseAI : MobAI
{
	private string mouseName;
	private string capMouseName;
	private float timeForNextRandomAction;
	private float timeWaiting;   

	protected override void Awake()
	{
		base.Awake();
		mouseName = mobName.ToLower();
		capMouseName = char.ToUpper(mouseName[0]) + mouseName.Substring(1);
		BeginExploring(MobExplore.Target.food);
	}

	protected override void UpdateMe()
	{
		if (health.IsDead || health.IsCrit || health.IsCardiacArrest) return;

		base.UpdateMe();
		MonitorExtras();
	}

	protected override void AIStartServer()
	{
		exploringStopped.AddListener(OnExploringStooped);
		fleeingStopped.AddListener(OnFleeingStopped);
	}

	void MonitorExtras()
	{
		//TODO eat cables if haven't eaten in a while

		timeWaiting += Time.deltaTime;
		if (timeWaiting > timeForNextRandomAction)
		{
			timeWaiting = 0f;
			timeForNextRandomAction = Random.Range(3f, 30f);

			DoRandomSqueek();
		}
	}

	public override void OnPetted(GameObject performer)
	{
		Squeak();
		StartFleeing(performer.transform, 3f);
	}

	protected override void OnAttackReceived(GameObject damagedBy)
	{
		Squeak();
		FleeFromAttacker(damagedBy, 5f);
	}

	void OnExploringStooped()
	{

	}

	void OnFleeingStopped()
	{
		BeginExploring(MobExplore.Target.food);
	}

	private void Squeak()
	{
		SoundManager.PlayNetworkedAtPos(
			"MouseSqueek",
			gameObject.transform.position,
			Random.Range(.6f, 1.2f));

		Chat.AddActionMsgToChat(
			gameObject,
			$"{capMouseName} squeaks!",
			$"{capMouseName} squeaks!");
	}

	private void DoRandomSqueek()
	{
	   Squeak();
	}
}