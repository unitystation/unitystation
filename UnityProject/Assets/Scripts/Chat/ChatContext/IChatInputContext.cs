using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IChatInputContext 
{
	/// <summary>
	/// This is channel tagged as ':h'
	/// Depends on current headset, antags, etc
	/// </summary>
	ChatChannel DefaultChannel { get; }
}
