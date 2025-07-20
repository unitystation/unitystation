using System;
using TMPro;
using UnityEngine;

public class AdminVoteUISub : MonoBehaviour
{
	public AdminVoteUI AdminVoteUI;

	public TMP_InputField InputField;

	public void SetUp(AdminVoteUI InAdminVoteUI)
	{
		AdminVoteUI = InAdminVoteUI;
	}

	public void OnDestroy()
	{
		if (AdminVoteUI != null)
		{
			AdminVoteUI.OnRemove(this);
		}
	}

	public void Remove()
	{
		Destroy(this.gameObject);
	}
}
