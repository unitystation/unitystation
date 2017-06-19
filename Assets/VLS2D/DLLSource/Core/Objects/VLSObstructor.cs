using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace PicoGames.VLS2D
{
    public enum ColliderReferenceType
    {
        None,
        _3D,
        _2D
    }

    [AddComponentMenu("VLS2D (2D Lights)/Obstructor"), ExecuteInEditMode]
    public class VLSObstructor : VLSBehaviour
    {
        [SerializeField]
        public ColliderReferenceType colliderReferenceType = ColliderReferenceType.None;

        [SerializeField]
        public Collider2D collider2DReference = null;
        [SerializeField]
        public Collider collider3DReference = null;

        [SerializeField, Range(4, 32)]
        public int circleResolution = 8;
        [SerializeField]
        public int polyColliderPathIndex = 0;
        
        protected virtual void OnDrawGizmos()
        {
            DebugBounds(Color.magenta);
            DebugShape(new Color(0.3f, 0.6f, 0.8f, 1f));
        }

        protected virtual void OnDrawGizmosSelected()
        {
            if(SHOW_NORMALS)
                DebugNormals(new Color(0.8f, 0.6f, 0.3f, 1f));
        }

        protected virtual void OnEnable()
        {
            if(localVertices.Count == 0)
                GenerateDefaultVertices();

            VLSViewer.AddObstructor(this);
        }

        protected virtual void OnDisable()
        {
            VLSViewer.RemoveObstructor(this);
            foreach (VLSLight light in VLSViewer.VisibleLights)
            {
                if (this.bounds.Overlaps(light.bounds))
                    light.SetDirty();
            }
        }

        protected virtual void Reset()
        {
            GenerateDefaultVertices();
        }

        public void UpdateFromReferencedCollider()
        {
            if(colliderReferenceType == ColliderReferenceType.None)
                return;

            if (colliderReferenceType == ColliderReferenceType._2D)
            {
                if (collider2DReference == null)
                    return;

                if(collider2DReference is PolygonCollider2D)
                    polyColliderPathIndex = Mathf.Clamp(polyColliderPathIndex, 0, (collider2DReference as PolygonCollider2D).pathCount - 1);

                VLSConverter.FromCollider2D(this, collider2DReference);
            }

            if (colliderReferenceType == ColliderReferenceType._3D)
            {
                if (collider3DReference == null)
                    return;

                VLSConverter.FromCollider3D(this, collider3DReference);
            }
        }

        protected override void Update()
        {
            base.Update();

            if (!isActive)
                return;
                       
            if (isDirty)
            {
                foreach (VLSLight light in VLSViewer.VisibleLights)
                    if (bounds.Overlaps(light.bounds))
                        light.SetDirty();
                
                isDirty = false;
            }
        }

        public override void Active(bool _isActive)
        {
            if (!isActive && _isActive)
            {
                //VLSViewer.VisibleObstructions.Add(this);

                foreach (VLSLight light in VLSViewer.VisibleLights)
                {
                    if (this.bounds.Overlaps(light.bounds))
                        light.SetDirty();
                }
            }

            //if (isActive && !_isActive)
            //    VLSViewer.VisibleObstructions.Remove(this);

            base.Active(_isActive);
        }

    }
}