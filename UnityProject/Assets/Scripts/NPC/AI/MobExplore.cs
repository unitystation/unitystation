using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;

/// <summary>
/// AI brain specifically trained to explore
/// the surrounding area for specific objects
/// </summary>
public class MobExplore : Agent
{
	public Component componentToFind;
}
