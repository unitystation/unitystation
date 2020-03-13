using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AdminTools
{
	public class AdminPlayerChat : MonoBehaviour
	{
		[SerializeField] private ChatScroll chatScroll = null;

		[SerializeField] private InputFieldFocus inputField = null;
		//Register to AdminPlayerList to get the onclick event and do this: PlayerList.Instance.ClientGetUnreadMessages(PlayerData.uid);

		public void OnPlayerSelect(AdminPlayerEntryData playerData)
		{

		}
	}
}
