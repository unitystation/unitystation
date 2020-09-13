using System;
using UnityEngine;

namespace UI.Animations
{
	public abstract class AnimationBase: MonoBehaviour
	{
		/// <summary>
		/// The animation plays when the object is enabled and active in hierarchy
		/// </summary>
		public abstract void OnEnable();
	}
}