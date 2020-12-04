using System.Collections;
using UnityEngine;
using NaughtyAttributes;
using Items.Magical;

namespace UI
{
	/// <summary>
	/// UI for the <see cref="ContractOfApprenticeship"/>.
	/// </summary>
	public class GUI_ContractOfApprenticeship : NetTab
	{
		[Space]
		[SerializeField, BoxGroup("Settings"), ReorderableList]
		private string[] waitingMessages = default;
		[Space]
		[SerializeField, Required] private NetPageSwitcher pageSwitcher = default;
		[Space]
		[SerializeField, Required] private NetPage pageSelectSchool = default;
		[SerializeField, Required] private NetPage pageWaiting = default;
		[SerializeField, Required] private NetPage pageTimeout = default;
		[SerializeField, Required] private NetPage pageApprentice = default;
		[Space]
		[SerializeField, Required] private NetLabel labelWaitingMessage = default;
		[Space]
		[SerializeField, Required] private NetLabel labelApprenticeName = default;
		[SerializeField, Required] private NetLabel labelWizardName = default;
		[SerializeField, Required] private NetLabel labelSchoolName = default;

		private ContractOfApprenticeship contract;

		private Coroutine updateWaitingMessageCoroutine;

		#region Lifecycle

		protected override void InitServer()
		{
			StartCoroutine(WaitForProvider());
		}

		private IEnumerator WaitForProvider()
		{
			while (Provider == null)
			{
				yield return WaitFor.EndOfFrame;
			}

			contract = Provider.GetComponent<ContractOfApprenticeship>();

			if (contract.WasUsed)
			{
				SetAsUsed();
			}
			else
			{
				contract.OnGhostRoleTimeout += OnGhostRoleTimeout;
				contract.OnApprenticeSpawned += SetAsUsed;
			}
		}

		#endregion

		public void SetAsUsed()
		{
			labelWizardName.SetValueServer(contract.BoundTo.Script.playerName);
			labelApprenticeName.SetValueServer(contract.Apprentice.Script.playerName);
			labelSchoolName.SetValueServer(contract.SelectedSchool.Name);

			ActivatePage(pageApprentice);
		}

		public void OnBtnSchoolSelect(int schoolIndex)
		{
			contract.SelectSchool(schoolIndex);

			ActivatePage(pageWaiting);
		}

		public void OnBtnWaitingCancel()
		{
			contract.CancelApprenticeship();

			ActivatePage(pageSelectSchool);
		}

		public void OnBtnTimeoutGoBack()
		{
			ActivatePage(pageSwitcher.DefaultPage);
		}

		public void OnBtnTimeoutRetry()
		{
			contract.CreateGhostRole();

			ActivatePage(pageWaiting);
		}

		private void ActivatePage(NetPage page)
		{
			pageSwitcher.SetActivePage(page);

			if (page == pageWaiting)
			{
				this.RestartCoroutine(UpdateWaitingMessage(), ref updateWaitingMessageCoroutine);
			}
			else
			{
				this.TryStopCoroutine(ref updateWaitingMessageCoroutine);
			}
		}

		private void OnGhostRoleTimeout()
		{
			ActivatePage(pageTimeout);
		}

		private IEnumerator UpdateWaitingMessage()
		{
			int i = 0;
			while (i < 30)
			{
				i++;
				yield return WaitFor.Seconds(2);
				labelWaitingMessage.SetValueServer(waitingMessages.PickRandom());
			}
		}
	}
}
