using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Unitystation.Options
{
    /// <summary>
    /// Handles the UI for Theme Options in the
    /// Options Screen.false ThemeManager for
    /// Theme switching operations
    /// </summary>
    public class ThemeOptions : MonoBehaviour
    {
        public Dropdown chatBubbleDropDown;

        void OnEnable()
        {
            Refresh();
        }

        void Refresh()
        {

        }
    }
}