using System;
using Cysharp.Threading.Tasks;
using US13.Systems.NtOS.Core;
using US13.UI.Core.Net;
using US13.UI.Core.Net.Elements.Dynamic;

namespace US13.UI.Core.NtOS
{
	public class TabNtOS : NetTab
	{
		private NtOS_Device currentDevice;
		private NetUIDynamicList outputEntries;

		private void Start()
		{

		}

		protected override void InitServer()
		{
			_ = WaitForProvider();
		}

		private async UniTask WaitForProvider()
		{
			while (Provider == null)
			{
				await UniTask.WaitForEndOfFrame();
			}

			currentDevice = Provider.GetComponent<NtOS_Device>();
		}
	}
}