using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

[System.Serializable]
public class OnCrossed : UnityEvent<RegisterPlayer>{};

[ExecuteInEditMode]
	public class RegisterItem : RegisterTile
	{
		public OnCrossed crossed;

		public void Cross(RegisterPlayer registerPlayer)
		{
			crossed.Invoke(registerPlayer);
		}

		private CustomNetTransform pushable;
		protected override void InitDerived()
		{
			pushable = GetComponent<CustomNetTransform>();
		}

		public override void UpdatePositionServer()
		{
			if ( !pushable )
			{
				base.UpdatePositionServer();
			}
			else
			{
				PositionS = pushable.ServerLocalPosition;
			}
		}
		public override void UpdatePositionClient()
		{
			if ( !pushable )
			{
				base.UpdatePositionClient();
			}
			else
			{
				PositionC = pushable.ClientLocalPosition;
			}
		}
	}
