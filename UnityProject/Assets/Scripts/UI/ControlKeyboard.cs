using UnityEngine;
using UnityEngine.UI;
using PlayGroup;

namespace UI
{
    public class ControlKeyboard : MonoBehaviour
    {
        private Button button;

        private void OnEnable()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }
            button.GetComponentInChildren<Text>().text = "QWERTY";
        }


        public void ChangeKeyboardInput()
        {
            if (PlayerManager.LocalPlayerScript != null)
            {
                PlayerMove plm = PlayerManager.LocalPlayerScript.playerMove;
                if (button.GetComponentInChildren<Text>().text == "QWERTY")
                {
                    plm.keyCodes = new KeyCode[] { KeyCode.Z, KeyCode.Q, KeyCode.S, KeyCode.D, KeyCode.UpArrow, KeyCode.LeftArrow, KeyCode.DownArrow, KeyCode.RightArrow };
                    button.GetComponentInChildren<Text>().text = "AZERTY";
                }
                else if (button.GetComponentInChildren<Text>().text == "AZERTY")
                {
                    plm.keyCodes = new KeyCode[] { KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.UpArrow, KeyCode.LeftArrow, KeyCode.DownArrow, KeyCode.RightArrow };
                    button.GetComponentInChildren<Text>().text = "QWERTY";
                }
            }
        }
    }
}
