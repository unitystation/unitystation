using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IInitialiseSystem
{
	public int Priority { get; }
	public void Initialize();
}
