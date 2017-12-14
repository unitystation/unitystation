using UnityEngine;

namespace Light2D.Examples
{
    public class MouseFollower : MonoBehaviour
    {
        private Vector2 _pressPos;
        public bool RightClickRotation;

        private void LateUpdate()
        {
            if (Input.GetMouseButtonDown(0))
            {
                _pressPos = Util.GetMousePosInUnits();
            }

            if (Input.GetMouseButton(0) && RightClickRotation)
            {
                var shift = Util.GetMousePosInUnits() - _pressPos;
                if (shift.sqrMagnitude > 0.1f * 0.1f)
                {
                    var angle = shift.AngleZ();
                    transform.rotation = Quaternion.Euler(0, 0, angle);
                }
            }
            else
            {
                Vector3 pos = Util.GetMousePosInUnits();
                pos.z = transform.position.z;
                transform.position = pos;
            }
        }
    }
}