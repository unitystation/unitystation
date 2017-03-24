using InputControl;
using PlayGroup;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class ShutterSwitchTrigger: InputTrigger
{

	public ShutterController[] shutters;

	public bool IsClosed { get; private set; }

	private Animator animator;

	void Start()
	{
		animator = GetComponent<Animator>();
	}

	public override void Interact()
	{
		if (!this.animator.GetCurrentAnimatorStateInfo(0).IsName("Switches_ShuttersUP")) {
			if (IsClosed) {
				CmdOpenShutters();
			} else {
				CmdCloseShutters();
			}
		}
	}

	[Command]
	public void CmdOpenShutters()
	{
		RpcOpenShutters();
	}

	[Command]
	public void CmdCloseShutters()
	{
		RpcCloseShutters();
	}

	[ClientRpc]
	public void RpcOpenShutters()
	{
		if (IsClosed) {
			IsClosed = false;
			foreach (var s in shutters) {
				s.Open();
			}
			animator.SetTrigger("activated");
		}
	}

	[ClientRpc]
	public void RpcCloseShutters()
	{
		if (!IsClosed) {
			IsClosed = true;
			foreach (var s in shutters) {
				s.Close();
			}
			animator.SetTrigger("activated");
		}
	}
}
