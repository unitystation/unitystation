using System;
using Systems.Antagonists;
using UnityEngine;
using UnityEngine.EventSystems;

public class AlienMouseInputController : MouseInputController
{
	private AlienPlayer alienPlayer;

	private void Awake()
	{
		alienPlayer = GetComponent<AlienPlayer>();
	}
}