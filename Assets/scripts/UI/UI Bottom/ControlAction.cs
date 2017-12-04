using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using PlayGroup;


namespace UI
{
    public class ControlAction : MonoBehaviour
    {

        public Sprite[] throwSprites;
        public Image throwImage;

        void Start()
        {
            UIManager.IsThrow = false;
        }

        /* 
		 * Button OnClick methods
		 */

        void Update()
        {
            CheckKeyboardInput();
        }

        void CheckKeyboardInput()
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
        }

        public void Resist()
        {
            SoundManager.Play("Click01");
            Debug.Log("Resist Button");
        }

        public void Drop()
        {
            var lps = PlayerManager.LocalPlayerScript;
            if (!lps || lps.canNotInteract()) return;
            var currentSlot = UIManager.Hands.CurrentSlot;
            var dropPos = lps.gameObject.transform.position;
            if (!currentSlot.CanPlaceItem()) return;
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
            currentSlot.Clear();
            //            }
            //Message
            lps.playerNetworkActions.DropItem(currentSlot.eventName);
            SoundManager.Play("Click01");
            Debug.Log("Drop Button");
        }

        private static bool isNotMovingClient(PlayerScript lps)
        {
            return !lps.isServer && !lps.playerMove.isMoving;
        }

        public void Throw()
        {
            var lps = PlayerManager.LocalPlayerScript;
            if (!lps || lps.canNotInteract()) return;

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