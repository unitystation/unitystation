using UnityEditor;
using UnityEngine;
using System.Collections;

namespace PicoGames.VLS2D
{
    public class VLSMenu : MonoBehaviour
    {
        [MenuItem("GameObject/VLS2D/Radial", false, 21)]
        static void CreateRadialLight()
        {
            VLSRadial l2D = VLSLight.CreateRadial();
            Selection.activeGameObject = l2D.gameObject;
        }

        [MenuItem("GameObject/VLS2D/RadialCS", false, 21)]
        static void CreateRadialCSLight()
        {
            VLSRadialCS l2D = VLSLight.CreateRadialCS();
            Selection.activeGameObject = l2D.gameObject;
        }

        //[MenuItem("Component/VLS2D (2D Lights)/Light Viewer", false, 30)]
        //static void AddLightViewerComponent()
        //{
        //    Selection.activeGameObject.AddComponent<VLSViewer>();
        //}
        //[MenuItem("Component/VLS2D (2D Lights)/Light Viewer", true, 30)]
        //static bool ValidateAddLightViewerComponent()
        //{
        //    return (Selection.activeGameObject.GetComponent<Camera>() != null && Selection.activeGameObject.GetComponent<VLSViewer>() == null);
        //}

        //[MenuItem("Component/VLS2D (2D Lights)/Obstructor", false, 31)]
        //static void AddObstructorComponent()
        //{
        //    Selection.activeGameObject.AddComponent<VLSObstructor>();
        //}
        //[MenuItem("Component/VLS2D (2D Lights)/Obstructor", true, 31)]
        //static bool ValidateAddObstructorComponent()
        //{
        //    return Selection.activeGameObject != null;
        //}
    }
}