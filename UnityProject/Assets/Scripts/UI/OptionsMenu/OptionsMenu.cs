using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unitystation.Options
{
    /// <summary>
    /// Main controller for the Options Screen
    /// It is spawned via managers on manager start
    /// and persists across scene changes
    /// </summary>
    public class OptionsMenu : MonoBehaviour
    {
        public static OptionsMenu Instance;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        //All the nav buttons in the left column
        private List<OptionsButton> optionButtons = new List<OptionsButton>();

        /// <summary>
        /// Register a nav button on start
        /// </summary>
        public void RegisterOptionButton(OptionsButton button)
        {
            optionButtons.Add(button);
        }

        public void ToggleButtonOn(OptionsButton button)
        {
            foreach (OptionsButton b in optionButtons)
            {
                if (b == button)
                {
                    b.Toggle(true);
                }
                else
                {
                    b.Toggle(false);
                }
            }
        }
    }
}