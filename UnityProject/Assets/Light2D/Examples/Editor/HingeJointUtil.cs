using System.Linq;
using UnityEngine;
using System.Collections;
using UnityEditor;

public class HingeJointUtil : MonoBehaviour
{
    [MenuItem("CONTEXT/HingeJoint2D/Anchor To Conn Acnchor")]
    private static void AnchorToConnAnchor(MenuCommand menuCommand)
    {
        var joint = (HingeJoint2D) menuCommand.context;
        var worldAnchor = joint.connectedBody.transform.TransformPoint(joint.connectedAnchor);
        joint.anchor = joint.transform.InverseTransformPoint(worldAnchor);
    }
}
