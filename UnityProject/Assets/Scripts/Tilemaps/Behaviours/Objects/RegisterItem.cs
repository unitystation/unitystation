using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

[System.Serializable]
public class OnCrossed : UnityEvent<RegisterPlayer>{};

[ExecuteInEditMode]
[RequireComponent(typeof(SpriteMatrixRotation))]
	public class RegisterItem : RegisterTile
	{
		public OnCrossed crossed;

		public void Cross(RegisterPlayer registerPlayer)
		{
			crossed.Invoke(registerPlayer);
		}

	}
