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

		[SyncVar(hook = nameof(OnChangeActiveAnimationID)), SerializeField]
		private int activeAnimationID = -1;
		[SerializeField]
		private GameObject animationsHolder;

		private void Awake()
		{
			animations.AddRange(animationsHolder.GetComponents<ILightAnimation>());
		}

		public override void OnStartClient()
		{
			base.OnStartClient();
			if (activeAnimationID != -1) PlayAnim(activeAnimationID);
		}

		public void StopAnim()
		{
			if (ActiveAnimation == null) return;
			ActiveAnimation.StopAnimation();
			ActiveAnimation = null;
			activeAnimationID = -1;
		}

		[Client]
		public void PlayAnim(ILightAnimation anim)
		{
			StopAnim();
			ActiveAnimation = anim;
			ActiveAnimation.StartAnimation();
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

		private void OnChangeActiveAnimationID(int oldState, int newState)
		{
			if (oldState == newState || activeAnimationID == newState) return;
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