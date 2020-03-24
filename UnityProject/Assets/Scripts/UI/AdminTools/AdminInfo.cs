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
		private IAdminInfo adminInfos;
		[Tooltip("The position offset from the center of the tracked object")]
		[SerializeField] private Vector2 offsetPosition;

		public Vector2 OffsetPosition => offsetPosition;

		public string StringInfo
		{
			get
			{
				if (adminInfos == null)
				{
					return "";
				}
				else
				{
					return adminInfos.AdminInfoString();
				}
			}
		}

		public override void OnStartServer()
		{
			base.OnStartServer();
			adminInfos = GetComponent<IAdminInfo>();
			AdminOverlay.ServerAddInfoPanel(netId, this);
		}
	}
}
