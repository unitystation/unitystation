using System;
using System.Collections.Generic;
using System.Linq;
using Shared.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using US13.Messages.Client;

namespace US13.Systems.Voting
{
	public class AdminVoteUI  : SingletonManager<AdminVoteUI>
	{
		public List<AdminVoteUISub> AdminVoteUISubs = new List<AdminVoteUISub>();

		public Transform TargetTransform;

		public AdminVoteUISub TargetPrefab;

		public TMP_InputField Title;

		public TMP_Text Text;

		public Button StopVoteButton;

		public override void Start()
		{
			base.Start();
			this.gameObject.SetActive(false);
		}

		public void OnAdd()
		{
			var  NewAdminVoteUISub = Instantiate(TargetPrefab, TargetTransform);
			AdminVoteUISubs.Add(NewAdminVoteUISub);
			NewAdminVoteUISub.SetUp(this);
		}

		public void OnRemove(AdminVoteUISub AdminVoteUISub)
		{
			if (AdminVoteUISubs.Contains(AdminVoteUISub))
			{
				AdminVoteUISubs.Remove(AdminVoteUISub);

			}
		}

		public void Close()
		{
			this.gameObject.SetActive(false);
		}


		public void OnStartPoll()
		{
			AdminRequestPoll.Send(Title.text, AdminVoteUISubs.Select(x => x.InputField.text).ToArray()  , false  );
			StopVoteButton.interactable = true;
		}


		public void ReceiveResult(string Result)
		{
			Text.text = Result;
			StopVoteButton.interactable = false;
		}

		public void OnEndPoll()
		{
			AdminRequestPoll.Send(Title.text, Array.Empty<string>() , true  );
		}
	}
}
