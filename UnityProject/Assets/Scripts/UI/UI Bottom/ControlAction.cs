using PlayGroup;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	public class ControlAction : MonoBehaviour
	{
		public Image throwImage;
		public Sprite[] throwSprites;

		private void Start()
		{
			UIManager.IsThrow = false;
		}

		/* 
		   * Button OnClick methods
		   */

		private void Update()
		{
			CheckKeyboardInput();
		}

		private void CheckKeyboardInput()
		{
			if (Input.GetKeyDown(KeyCode.Q))
			{
				Drop();
			}

			if (Input.GetKeyDown(KeyCode.X))
			{
				UIManager.Hands.Swap();
			}

			if (Input.GetKeyDown(KeyCode.E))
			{
				UIManager.Hands.Use();
			}

            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                UIManager.Intent.IntentHotkey(0);
            }

            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                UIManager.Intent.IntentHotkey(1);
            }

            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                UIManager.Intent.IntentHotkey(2);
            }

            if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                UIManager.Intent.IntentHotkey(3);
            }
        }

		public void Resist()
		{
			SoundManager.Play("Click01");
			Debug.Log("Resist Button");
		}

		public void Drop()
		{
			PlayerScript lps = PlayerManager.LocalPlayerScript;
			if (!lps || lps.canNotInteract())
			{
				return;
			}
			UI_ItemSlot currentSlot = UIManager.Hands.CurrentSlot;
			Vector3 dropPos = lps.gameObject.transform.position;
			if (!currentSlot.CanPlaceItem())
			{
				return;
			}
			//            if ( isNotMovingClient(lps) )
			//            {
			//               // Full client simulation(standing still)
			//                var placedOk = currentSlot.PlaceItem(dropPos);
			//                if ( !placedOk )
			//                {
			//                    Debug.Log("Client dropping error");
			//                }
			//            }
			//            else
			//            {
			//Only clear slot(while moving, as prediction is shit in this situation)
			GameObject dropObj = currentSlot.Item;
			CustomNetTransform cnt = dropObj.GetComponent<CustomNetTransform>();
			//It is converted to LocalPos in transformstate struct
			cnt.AppearAtPosition(PlayerManager.LocalPlayer.transform.position);
			currentSlot.Clear();
			//            }
			//Message
			lps.playerNetworkActions.RequestDropItem(currentSlot.eventName, PlayerManager.LocalPlayer.transform.position ,false);
			SoundManager.Play("Click01");
			Debug.Log("Drop Button");
		}

		private static bool isNotMovingClient(PlayerScript lps)
		{
			return !lps.isServer && !lps.playerMove.isMoving;
		}

		public void Throw()
		{
			PlayerScript lps = PlayerManager.LocalPlayerScript;
			if (!lps || lps.canNotInteract())
			{
				return;
			}

			SoundManager.Play("Click01");
			Debug.Log("Throw Button");

			if (!UIManager.IsThrow)
			{
				UIManager.IsThrow = true;
				throwImage.sprite = throwSprites[1];
			}
			else
			{
				UIManager.IsThrow = false;
				throwImage.sprite = throwSprites[0];
			}
		}
	}
}