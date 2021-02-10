using Newtonsoft.Json.Linq;
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

	/*
	 * 1. The integer 0 on the experience level shall represent a total newbie to SS13.
	 * 2. The integer 1 on the experience level shall represent a newbie to UnityStation.
	 * 3. the integer 2 on the experience level shall represent someone who is fairly experienced.
	 */

	private string Text;
	public GameObject gObject = null;
	int type;

	/*
	 * 1. The integer 0 shall represent a held object.
	 * 2. The integer 1 shall represent a held class of objects.
	 * 3. The integer 2 shall represent beign near a world object.
	 * 4. The integer 3 shall represent opening a GUI.
	 * 5. The integer 4 shall represent hazardous areas.
	 * 6. The integer 5 shall represent time from joining the server.
	 * 7. The integer 6 shall represent player actions.
	 * 8. The integer 7 shall represent nearby player health states.
	 * 9. The integer 8 shall represent changing intents.
	 * 10. The integer 9 shall represent switching states on an object.
	 */

	public Advice(int xpLevel, int typet, string t, GameObject gobj = null)
	{
		experienceLevel = xpLevel;
		type = typet;
		Text = t;
		gObject = gobj;
	}
}