using System;
using UnityEngine;
using UnityEngine.UI;
using Objects;

namespace AdminTools
{
	/// <summary>
	/// Displays customized data for viewing by admins via
	/// the Admin Overlay canvas
	/// </summary>
	public class AdminOverlayPanel : MonoBehaviour
	{
		[SerializeField] private Text displayText = null;
		private UniversalObjectPhysics targetObjBehaviour;
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
			Transform objectToFollow, Vector2 followOffset)
		{
			if (objectToFollow == null) return;

			targetObjBehaviour = objectToFollow.GetComponent<UniversalObjectPhysics>();
			target = objectToFollow.transform;

			cam = Camera.main;
			SetText(text);
			this.adminOverlay = adminOverlay;
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
			for (int i = 0; i < lines.Length; i++)
			{
				if (i == lines.Length - 1)
				{
					newString += lines[i];
				}
				else
				{
					newString += lines[i] + Environment.NewLine;
				}
			}

			displayText.text = newString;
		}

		private void OnEnable()
		{
			UpdateManager.Add(CallbackType.FIXED_UPDATE, FixedUpdateMe);
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.FIXED_UPDATE, FixedUpdateMe);
		}

		void FixedUpdateMe()
		{
			if (target != null)
			{
				FollowTarget();
			}
		}

		void FollowTarget()
		{
			// check container:
			if(targetObjBehaviour != null)
			{
				if (targetObjBehaviour.ContainedInObjectContainer != null)
				{
					if (targetObjBehaviour.ContainedInObjectContainer.transform != target)
					{
						target = targetObjBehaviour.ContainedInObjectContainer.transform;
					}
				}
				else
				{
					if (target != targetObjBehaviour.transform)
					{
						target = targetObjBehaviour.transform;
					}
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
