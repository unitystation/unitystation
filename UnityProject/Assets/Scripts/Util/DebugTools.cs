    using UnityEngine;

public class DebugTools
    {
        

        /// Utility from stackoverflow
        public static Color HexToColor(string hex)
        {
            hex = hex.Replace ("0x", "");//in case the string is formatted 0xFFFFFF
            hex = hex.Replace ("#", "");//in case the string is formatted #FFFFFF
            byte a = 255;//assume fully visible unless specified in hex
            byte r = byte.Parse(hex.Substring(0,2), System.Globalization.NumberStyles.HexNumber);
            byte g = byte.Parse(hex.Substring(2,2), System.Globalization.NumberStyles.HexNumber);
            byte b = byte.Parse(hex.Substring(4,2), System.Globalization.NumberStyles.HexNumber);
            //Only use alpha if the string has enough characters
            if(hex.Length == 8){
                a = byte.Parse(hex.Substring(6,2), System.Globalization.NumberStyles.HexNumber);
            }
            return new Color32(r,g,b,a);
        }
    }
public static class DrawArrow
{
    public static void ForGizmo(Vector3 pos, Vector3 direction, float arrowHeadLength = 0.25f, float arrowHeadAngle = 20.0f)
    {
        if ( direction == Vector3.zero )
        {
            return;
        }
        Gizmos.DrawRay(pos, direction);

        Quaternion lookRotation = Quaternion.LookRotation (direction);
        Vector3 right = lookRotation * Quaternion.Euler (arrowHeadAngle, 0, 0) * Vector3.back;
        Vector3 left = lookRotation * Quaternion.Euler (-arrowHeadAngle, 0, 0) * Vector3.back;
        Vector3 up = lookRotation * Quaternion.Euler (0, arrowHeadAngle, 0) * Vector3.back;
        Vector3 down = lookRotation * Quaternion.Euler (0, -arrowHeadAngle, 0) * Vector3.back;
        Gizmos.color = Gizmos.color;
        Gizmos.DrawRay (pos + direction, right * arrowHeadLength);
        Gizmos.DrawRay (pos + direction, left * arrowHeadLength);
        Gizmos.DrawRay (pos + direction, up * arrowHeadLength);
        Gizmos.DrawRay (pos + direction, down * arrowHeadLength);
    }
}