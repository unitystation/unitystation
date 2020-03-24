using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

namespace AdminTools
{
	/// <summary>
	/// Add Admin info to any networked obj that needs to have information displayed
	/// on the admins overlay canvas. Add the IAdminInfo interface to each component
	/// where information needs to be gathered from. This component will then handle
	/// the communication to the admin clients
	/// </summary>
	public class AdminInfo : NetworkBehaviour
	{
		private List<IAdminInfo> adminInfos = new List<IAdminInfo>();

		public override void OnStartServer()
		{
			base.OnStartServer();
			adminInfos.Clear();
			adminInfos = GetComponents<IAdminInfo>().ToList();
		}
	}
}
