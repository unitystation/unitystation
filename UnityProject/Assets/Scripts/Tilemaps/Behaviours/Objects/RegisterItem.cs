using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;


[ExecuteInEditMode]
	public class RegisterItem : RegisterTile
	{
		public UnityEvent OnCrossed;

		[HideInInspector]
		public RegisterPlayer CrossedRegisterPlayer;

		public void Cross(RegisterPlayer registerPlayer)
		{
			CrossedRegisterPlayer = registerPlayer;
			OnCrossed?.Invoke();
		}

	}
