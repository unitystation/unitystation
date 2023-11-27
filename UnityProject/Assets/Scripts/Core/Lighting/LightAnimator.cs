using System.Collections.Generic;
using Logs;
using Mirror;
using UnityEngine;

namespace Core.Lighting
{
	public class LightAnimator : NetworkBehaviour, IRightClickable
	{
		private List<ILightAnimation> animations = new List<ILightAnimation>();
		public ILightAnimation ActiveAnimation { get; private set; } = null;

		[SyncVar(hook = nameof(SyncActiveAnimationID)), SerializeField]
		private int activeAnimationID = -1;
		[SerializeField]
		private GameObject animationsHolder;

		private void Awake()
		{
			animations.AddRange(animationsHolder.GetComponents<ILightAnimation>());
		}


		private void StopAnim()
		{
			if (ActiveAnimation == null) return;
			ActiveAnimation.StopAnimation();
			ActiveAnimation = null;
			activeAnimationID = -1;
		}

		private void PlayAnim(ILightAnimation anim)
		{
			StopAnim();
			ActiveAnimation = anim;
			ActiveAnimation.StartAnimation();
		}

		public void ServerPlayAnim(int animID)
		{
			SyncActiveAnimationID(activeAnimationID, animID);
		}

		public void ServerStopAnim()
		{
			SyncActiveAnimationID(activeAnimationID, -1);
		}



		public void PlayAnim(int animID)
		{
			foreach (var anim in animations)
			{
				if (anim.ID == animID) PlayAnim(anim);
				break;
			}
		}

		public void PlayAnimNetworked(int animID)
		{
			foreach (var anim in animations)
			{
				if (anim.ID != animID) continue;
				PlayAnim(anim);
				activeAnimationID = animID;
				return;
			}
			Loggy.LogError($"[PlayAnimNetworked] - animation with {animID} id not found.");
		}

		public void StopAnims()
		{
			StopAnim();
		}

		private void SyncActiveAnimationID(int oldState, int newState)
		{
			activeAnimationID = newState;
			if (newState == -1)
			{
				StopAnim();
				return;
			}

			foreach (var anim in animations)
			{
				if (anim.ID != newState) continue;
				PlayAnim(anim);
				break;
			}
		}

		public RightClickableResult GenerateRightClickOptions()
		{
			var result = new RightClickableResult();
			result.AddAdminElement("Start emergency animation", () => { PlayAnimNetworked(0); });
			result.AddAdminElement("Start flicker animation", () => { PlayAnimNetworked(1); });
			result.AddAdminElement("Stop animations", StopAnims);
			return result;
		}
	}
}