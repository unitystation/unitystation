using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Light2D
{
    /// <summary>
    /// Some configuration for LightingSystem. Containd in lighting system prefab, destroyed after ininial setup.
    /// </summary>
    public class LightingSystemPrefabConfig : MonoBehaviour
    {
        public Material AmbientLightComputeMaterial;
        public Material LightOverlayMaterial;
        public Material BlurMaterial;
    }
}
