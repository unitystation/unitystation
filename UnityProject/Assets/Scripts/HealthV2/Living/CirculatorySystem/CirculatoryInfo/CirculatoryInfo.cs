using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CirculatoryInfo : ScriptableObject
{
	//General data that the circulatory system needs.
	public int BLOOD_MAX = 2000;
	public int BLOOD_SLIME_SPLIT = 1120;
	public int BLOOD_NORMAL = 560;
	public int BLOOD_SAFE = 460;
	public int BLOOD_OKAY = 336;
	public int BLOOD_BAD = 224;

	//If we reach critical, the organism will quickly accumalte damage.
	public int BLOOD_CRITICAL = 168;

	//Default value for the amount of blood in the circulatory system.
	public int BLOOD_DEFAULT = 560;

	//Important to note - these values will not determine the true heartrate of the organism.
	//That is something that an organ has will do. These represent the ideal conditions for the
	//circulatory system itself.
	//These will be used as suggestions by the organism responsible for circulating blood.

	//The maximum heartrate of the circulatory system. If this is surpassed, the organism may suffer damage.
	public int HEARTRATE_MAX = 200;

	//The ideal resting heartrate of the organism.
	public int HEARTRATE_NORMAL = 55;

	//The minimum heartrate of the circulatory system. If this is reached, the organism will enter cardiac arrest.
	public int HEARTRATE_MIN = 2;

	//The maximum amount of reagent we can have in our circulatory system.
	//This has no unit, and is an arbitrary amount. Essentially, it will be a value that
	//is purely used for gameplay interaction, with no tangible real world component.
	public int BLOOD_REAGENT_MAX = 200;

	//The normal amount of reagent in our blood.
	public int BLOOD_REAGENT_NORMAL = 100;

	//How much of our blood reagent is consumed per heartbeat.
	//Organs and body parts may consume additional.
	//How this is refilled isn't handled by the circulatory system.
	public float BLOOD_REAGENT_CONSUME_PER_BEAT = 0.1f;
}
