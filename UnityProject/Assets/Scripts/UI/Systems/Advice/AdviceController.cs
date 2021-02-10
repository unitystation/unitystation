using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewBehaviourScript : MonoBehaviour
{

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

public class Advice
{

	private int experienceLevel;
	private TriggerCondition tCondition;
	private string Text;

	public Advice(int xpLevel, TriggerCondition trigger, string t)
	{
		experienceLevel = xpLevel;
		tCondition = trigger;
		Text = t;
	}
}

public class TriggerCondition
{

	public TriggerCondition()
	{

	}
}