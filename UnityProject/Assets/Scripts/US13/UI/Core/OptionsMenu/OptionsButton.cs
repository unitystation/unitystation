using UnityEngine;
using UnityEngine.EventSystems;
using US13.Core.Addressables;
using US13.Managers;

namespace US13.UI.Core.OptionsMenu
{
    /// <summary>
    /// Controls the navigation button state
    /// </summary>
    public class OptionsButton : MonoBehaviour, IPointerDownHandler
    {
        [SerializeField]
        private GameObject selectedLine = null;
        [SerializeField]
        [Tooltip("This is the actual window that contains all of the" +
            " GUI buttons/inputs for this particular settings option")]
        private GameObject contentWindow = null;
        public bool IsActive{ get { return selectedLine.activeSelf; } }

        public void OnPointerDown(PointerEventData data)
        {
            _ = SoundManager.Play(CommonSounds.Instance.Click01);
            OptionsMenu.Instance.ToggleButtonOn(this);
        }

        public void Toggle(bool activeState)
        {
            selectedLine.SetActive(activeState);
            if (contentWindow) contentWindow.SetActive(activeState);
        }

        public void ResetDefaults()
        {
            contentWindow.BroadcastMessage("ResetDefaults", SendMessageOptions.DontRequireReceiver);
        }
    }
}
