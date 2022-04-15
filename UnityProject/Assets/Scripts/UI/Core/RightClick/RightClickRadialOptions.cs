using System;
using ScriptableObjects;
using UnityEngine;

namespace UI.Core.RightClick
{
	[CreateAssetMenu(fileName = "RightClickRadialOptions", menuName = "Interaction/Right Click Radial Options")]
	public class RightClickRadialOptions : ScriptableObject
	{
		[SerializeField]
		[Tooltip("Should the radial show the outer action ring.")]
		private bool showActionRadial;

		[SerializeField]
		[Tooltip("Should the branch to the radial be visible.")]
		private bool showBranch;

		public bool ShowActionRadial => showActionRadial;

		public bool ShowBranch => showBranch;
	}
}