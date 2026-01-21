using Core.Highlight;
using Mirror;
using Player;
using UnityEngine;

namespace PlayerSpritesStuff
{
	public class HeadRotatable : NetworkBehaviour
	{

		public Rotatable Rotatable;

		public PlayerSprites PlayerSprites;

		public OrientationEnum Cdirection;
		[HideInInspector, SyncVar(hook = nameof(SyncServerDirection))]
		public OrientationEnum SynchroniseCurrentDirection;

		private void SyncServerDirection(OrientationEnum oldDir, OrientationEnum dir)
		{
			if (isOwned)
			{
				return;
			}
			//Seems like headless is running the hook when it shouldn't be
			//(Mirror bug or our custom code broke something?)
			if (CustomNetworkManager.IsHeadless) return;


			SetDirectionInternal(oldDir, dir);
		}

		public void SetDirectionInternal(OrientationEnum oldDir, OrientationEnum dir)
		{
			switch (Rotatable.CurrentDirection )
			{
				case OrientationEnum.Up_By0:
					if (dir == OrientationEnum.Down_By180)
					{
						dir = OrientationEnum.Left_By90;
					}
					break;
				case OrientationEnum.Down_By180:
					if (dir == OrientationEnum.Up_By0)
					{
						dir = OrientationEnum.Right_By270;
					}
					break;
				case OrientationEnum.Left_By90:
					if (dir == OrientationEnum.Right_By270)
					{
						dir = OrientationEnum.Down_By180;
					}
					break;
				case OrientationEnum.Right_By270:
					if (dir == OrientationEnum.Left_By90)
					{
						dir = OrientationEnum.Down_By180;
					}
					break;
			}



			Cdirection = dir;
			SynchroniseCurrentDirection = dir;
			PlayerSprites.OnDirectionChangeHead(dir);

			if (oldDir != dir)
			{
				if (
#if UNITY_EDITOR
					Application.isPlaying &&
#endif
					CustomNetworkManager.IsServer == false && isOwned)
				{
					CmdChangeDirection(dir);
				}

				Highlight.UpdateCurrentHighlight();
			}
		}

		//client requests the server to change serverDirection
		[Command]
		private void CmdChangeDirection(OrientationEnum direction)
		{
			SetDirectionInternal(Cdirection,direction);
		}

		public void SetFaceDirectionLocalVector(Vector2Int direction)
		{
			SetDirectionInternal(Cdirection, direction.ToOrientationEnum());
		}


	}

}
