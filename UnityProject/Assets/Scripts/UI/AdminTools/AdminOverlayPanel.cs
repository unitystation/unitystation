using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AdminTools
{
	/// <summary>
	/// Displays customized data for viewing by admins via
	/// the Admin Overlay canvas
	/// </summary>
	public class AdminOverlayPanel : MonoBehaviour
	{
		[SerializeField] private Text displayText;
		private ObjectBehaviour objectToFollow;
		private AdminOverlay adminOverlay;
		private Transform target;
		private Vector3 followOffset;
		private Camera cam;

		/// <summary>
		/// Set the Panel up so it can display the text and follow the target
		/// </summary>
		/// <param name="text"> Text to display in the info box.
		/// Character limit for each line is 20 with a max of 3 lines. Anything past
		/// this point is cut off</param>
		/// <param name="objectToFollow">The object behaviour of the object being followed.
		/// The object behaviour allows the checking of parent containers.</param>
		/// <param name="followOffset">The offset in world meters from the centre of the
		/// following target</param>
		public void SetAdminOverlayPanel(string text, AdminOverlay adminOverlay,
			ObjectBehaviour objectToFollow, Vector2 followOffset)
		{
			if (objectToFollow == null) return;

			target = objectToFollow.transform;
			cam = Camera.main;
			SetText(text);
			this.adminOverlay = adminOverlay;
			this.objectToFollow = objectToFollow;
			this.followOffset = followOffset;
			gameObject.SetActive(true);
		}

		void SetText(string text)
		{
			string[] lines = text.Split(
				new[] { "\r\n", "\r", "\n" },
				StringSplitOptions.None
			);

			var newString = "";
			for (int i = 0; i < lines.Length && i < 3; i++)
			{
				if (lines[i].Length > 20)
				{
					lines[i] = lines[i].Substring(0, 20) + "..";
				}

				newString += lines[i] + Environment.NewLine;
			}

			displayText.text = newString;
		}

		private void OnEnable()
		{
			UpdateManager.Add(CallbackType.LATE_UPDATE, FixedUpdateMe);
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.LATE_UPDATE, FixedUpdateMe);
		}

		void FixedUpdateMe()
		{
			if (target != null && objectToFollow != null)
			{
				FollowTarget();
			}
		}

		void FollowTarget()
		{
			// check container:
			if (objectToFollow.parentContainer != null)
			{
				if (objectToFollow.parentContainer.transform != target)
				{
					target = objectToFollow.parentContainer.transform;
				}
			}
			else
			{
				if (target != objectToFollow.transform)
				{
					target = objectToFollow.transform;
				}
			}

			Vector3 viewPos = cam.WorldToScreenPoint(target.position + followOffset);
			transform.position = viewPos;
		}

		public void ReturnToPool()
		{
			adminOverlay.ReturnToPool(this);
		}
	}
}
