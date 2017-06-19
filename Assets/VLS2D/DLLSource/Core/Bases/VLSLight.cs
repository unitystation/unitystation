using UnityEngine;
using System.Collections;

namespace PicoGames.VLS2D
{
    [ExecuteInEditMode, RequireComponent(typeof(MeshFilter), typeof(MeshRenderer)), DisallowMultipleComponent]
    public abstract class VLSLight : VLSBehaviour
    {
        public bool IsStatic 
        { 
            get 
            { return isStatic; } 
            set 
            { 
                isStatic = value; 

                if (!value) 
                    SetDirty();
            } 
        }

        public Color Color
        {
            get 
            { 
                return color; 
            } 
            
            set 
            { 
                color = value;

                if(Application.isPlaying && isStatic)
                {
                    UpdateColors();
                    buffer.UpdateColors();
                }
                else
                    SetDirty(); 
            } 
        }

        public Material Material { get { return material; } set { material = value; SetDirty(); } }
        public VLSMeshBuffer Buffer { get { return buffer; } }
        public LayerMask ShadowLayer { get { return shadowLayer; } set { shadowLayer = value; SetDirty(); } }

        private static Material defaultLightMaterial = null;
        public static Material GetDefaultLightMaterial()
        {
            if (defaultLightMaterial == null)
                defaultLightMaterial = Resources.Load<Material>("Materials/Radial.mat");

            return defaultLightMaterial;
        }

        [SerializeField, HideInInspector]
        private bool isStatic = false;
        [SerializeField, HideInInspector]
        private Color color = new Color(0.8f, 0.6f, 0.3f, 1f);
        [SerializeField, HideInInspector]
        private Material material = null;
        [SerializeField, HideInInspector]
        protected LayerMask shadowLayer = -1;

        [SerializeField, HideInInspector]
        private int sortingLayerID = 0;
        [SerializeField, HideInInspector]
        private string sortingLayerName = "";
        [SerializeField, HideInInspector]
        private int sortingOrder = 0;
        
        [SerializeField, HideInInspector]
        protected VLSMeshBuffer buffer;
        [SerializeField, HideInInspector]
        protected bool flagUpdate = true;
        [SerializeField, HideInInspector]
        protected MeshRenderer mRenderer;
        [SerializeField, HideInInspector]
        private MeshFilter mFilter;
        [SerializeField, HideInInspector]
        private bool isInitialized = false;
        
        protected Material GetLightMaterial()
        {
            if (material == null)
                material = GetDefaultLightMaterial();

            return material;
        }

        protected virtual void OnDrawGizmos()
        {
            DebugBounds(Color.magenta);
            DebugShape(new Color(color.r, color.g, color.b, 1f));
        }

        protected virtual void OnDrawGizmosSelected()
        {
            DebugShape(new Color(color.r, color.g, color.b, 1f), true);
        }

        protected override void DebugShape(Color _color, bool _force = false)
        {
            if (!_force && !VLSDebug.IsModeActive(VLSDebugMode.Geometry))
                return;

            base.DebugShape(new Color(_color.r, _color.g, _color.b, 0.1f), _force);

            Gizmos.color = _color;
            if (buffer.VertexCount > 0)
                for(int i = 0; i < buffer.VertexCount - 1; i++)
                    Gizmos.DrawLine(transform.TransformPoint(buffer.vertices[i].position), transform.TransformPoint(buffer.vertices[(i + 1) % (buffer.VertexCount - 1)].position));

        }

        protected virtual void OnEnable()
        {
            VLSViewer.AddVLSLight(this);

            if (gameObject.isStatic)
                isStatic = true;

            flagUpdate = true;
        }

        protected virtual void OnDisable()
        {
            VLSViewer.RemoveVLSLight(this);
        }
        
        protected override void Awake()
        {
            if (mFilter == null)
                mFilter = GetComponent<MeshFilter>();
            mFilter.hideFlags = HideFlags.HideInInspector;

            if (mRenderer == null)
                mRenderer = GetComponent<MeshRenderer>();

            mRenderer.receiveShadows = false;
            mRenderer.castShadows = false;
            //mRenderer.hideFlags = HideFlags.HideInInspector;
            
            buffer = new VLSMeshBuffer(mFilter);

            base.Awake();

            isInitialized = false;
        }

        protected virtual void Reset()
        {
            if (mFilter == null)
                mFilter = GetComponent<MeshFilter>();
            mFilter.hideFlags = HideFlags.HideInInspector;

            if (mRenderer == null)
                mRenderer = GetComponent<MeshRenderer>();

            mRenderer.receiveShadows = false;
            mRenderer.castShadows = false;

            buffer = new VLSMeshBuffer(mFilter);
            isInitialized = false;
        }

        protected virtual void Start()
        {
            UpdateLight();
        }

        public override void SetDirty()
        {
            if (!Application.isPlaying || !isStatic)
                base.SetDirty();
        }

        public override void Active(bool _isActive)
        {
            if (!isActive && _isActive)
                isDirty = true;
            
            //if (isActive && !_isActive)
            //    VLSViewer.VisibleLights.Remove(this);

            if (!_isActive && (Application.isPlaying && !isStatic))
                buffer.Clear();

            base.Active(_isActive);
        }
                
        public abstract void UpdateVertices();
        public abstract void UpdateUVs();
        public abstract void UpdateTriangles();

        public void UpdateLight()
        {
            if (Application.isPlaying && isStatic && isInitialized)
                return;
            
            if (!isActive || !isDirty)
                return;

            if (mRenderer == null)
                return;

            mRenderer.sortingLayerID = sortingLayerID;
            mRenderer.sortingOrder = sortingOrder;
            
            UpdateVertices();

            UpdateTriangles();
            UpdateUVs();

            UpdateColors();
            UpdateNormals();

            buffer.Apply();
            mRenderer.material = GetLightMaterial();
                        
            isDirty = false;
            isInitialized = true;
        }

        public void LookAt(Transform _target)
        {
            LookAt(_target.position);
        }
        public void LookAt(Vector3 _target)
        {
            transform.rotation = Quaternion.FromToRotation(-Vector2.up, _target - transform.position);
        }

        protected virtual void UpdateColors()
        {
            if(buffer.colors.Length != buffer.VertexCount)
                buffer.colors = new Color32[buffer.VertexCount];

            for (int i = 0; i < buffer.VertexCount; i++)
                buffer.colors[i] = color;
        }

        protected virtual void UpdateNormals()
        {
            if (buffer.normals.Length != buffer.VertexCount)
                buffer.normals = new Vector3[buffer.VertexCount];

            for (int i = 0; i < buffer.VertexCount; i++)
                buffer.normals[i].Set(0, 0, -1);
        }
        
        public static VLSRadial CreateRadial()
        {
            GameObject go = new GameObject("VLS2D Radial", typeof(VLSRadial));
            VLSRadial l2d = go.GetComponent<VLSRadial>();

            l2d.transform.localScale = Vector3.one * VLSGlobals.DEFAULT_LIGHT_SCALE;
            l2d.ShadowLayer = VLSGlobals.DEFAULT_LIGHT_SHADOW_LAYER;
            l2d.gameObject.layer = VLSGlobals.DEFAULT_LIGHT_LAYER;

            return l2d;
        }

        public static VLSRadialCS CreateRadialCS()
        {
            GameObject go = new GameObject("VLS2D RadialCS", typeof(VLSRadialCS));
            VLSRadialCS l2d = go.GetComponent<VLSRadialCS>();

            l2d.transform.localScale = Vector3.one * VLSGlobals.DEFAULT_LIGHT_SCALE;
            l2d.ShadowLayer = VLSGlobals.DEFAULT_LIGHT_SHADOW_LAYER;
            l2d.gameObject.layer = VLSGlobals.DEFAULT_LIGHT_LAYER;

            return l2d;
        }
    }
}