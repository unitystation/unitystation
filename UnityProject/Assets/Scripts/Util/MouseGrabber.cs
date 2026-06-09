using UnityEngine;
using US13.Core.Input_System;
using US13.Managers.UpdateManager;

namespace Util
{
	public class MouseGrabber : MonoBehaviour
	{
		public bool SnapPosition = false;


		public Vector3 OffsetRound = new Vector3(-0f, -0f, 0);

		public void OnDisable()
		{
			UpdateManager.Remove(CallbackType.UPDATE,UpdateMe );
		}

		public void OnEnable()
		{
			UpdateManager.Add(CallbackType.UPDATE,UpdateMe );
		}

		public void UpdateMe()
		{
			if (SnapPosition)
			{
				var pos = MouseUtils.MouseToWorldPos();
				pos += OffsetRound;
				pos = pos.RoundToInt();
				pos -= OffsetRound;
				this.transform.position = pos;
			}
			else
			{
				this.transform.position = MouseUtils.MouseToWorldPos();
			}

		}
	}
}
