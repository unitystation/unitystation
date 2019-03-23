using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;


[ExecuteInEditMode]
	public class RegisterItem : RegisterTile
	{
		public delegate void Crossed();
		public event Crossed OnCrossed;

		[HideInInspector]
		public RegisterPlayer CrossedRegisterPlayer;

		public void Cross(ref RegisterPlayer registerPlayer)
		{
			CrossedRegisterPlayer = registerPlayer;
			OnCrossed?.Invoke();
		}

	}
