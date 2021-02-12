using UnityEngine;

public class Advice
{

	private experienceLevel xpLevel;

	private string text;
	public GameObject gObject = null;

	TriggerCondition type;
	

	public Advice(experienceLevel _xpLevel, TriggerCondition _type, string _text, GameObject gobj = null)
	{
		xpLevel = _xpLevel;
		type = _type;
		text = _text;
		gObject = gobj;
	}
}

public enum experienceLevel
{
	NEWBIE = 0,
	UNITYNEW = 1,
	EXPERIENCED = 2
}

public enum TriggerCondition
{
	HELDOBJECT = 0
}