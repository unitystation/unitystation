using UnityEngine;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// AI brain for mice
/// used to get hunted by Runtime and squeak
/// </summary>
public class MouseAI : MobAI
{
    private float timeForNextRandomAction;
    private float timeWaiting;   

    protected override void UpdateMe()
	{
		if (health.IsDead || health.IsCrit || health.IsCardiacArrest) return;

		base.UpdateMe();
		MonitorExtras();
	}

    void MonitorExtras()
	{
		//TODO eat cables if haven't eaten in a while

		if (IsPerformingTask) return;

		timeWaiting += Time.deltaTime;
		if (timeWaiting > timeForNextRandomAction)
		{
			timeWaiting = 0f;
			timeForNextRandomAction = Random.Range(1f,30f);

			DoRandomAction(Random.Range(1,2));
		}
	}

    public override void OnPetted(GameObject performer)
    {
        Squeak();
        StartFleeing(performer.transform);
    }

    protected override void OnAttackReceived(GameObject damagedBy)
    {
        Squeak();
        StartFleeing(damagedBy.transform, 10f);
    }

    private void Squeak()
    {
        SoundManager.PlayNetworkedAtPos("MouseSqueek", 
                                        gameObject.transform.position,
                                        Random.Range(.6f,1.2f));
    }

    private void DoRandomAction(int randAction)
    {
        switch (randAction)
        {
            case 1:
                Squeak();
                break;
            case 3:
                BeginExploring(exploreDuration: 10f);
                break;
        }
    }
}