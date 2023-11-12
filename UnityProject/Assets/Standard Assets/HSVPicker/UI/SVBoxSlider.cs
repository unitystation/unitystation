using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(BoxSlider), typeof(RawImage)), ExecuteInEditMode()]
public class SVBoxSlider : MonoBehaviour
{
    public ColorPicker picker;

    private BoxSlider slider;
    private RawImage image;

    private ComputeShader compute;
    private int kernelID;
    private RenderTexture renderTexture;
    private int textureWidth = 100;
    private int textureHeight = 100;

    private float lastH = -1;
    private bool listen = true;

    [SerializeField] private bool overrideComputeShader = false;
    private bool supportsComputeShaders = false;

    public RectTransform rectTransform
    {
        get
        {
            return transform as RectTransform;
        }
    }

    private void Awake()
    {
        slider = GetComponent<BoxSlider>();
        image = GetComponent<RawImage>();
        if(Application.isPlaying)
        {
            supportsComputeShaders = SystemInfo.supportsComputeShaders; //check for compute shader support

            #if PLATFORM_ANDROID
            supportsComputeShaders = false; //disable on android for now. Issue with compute shader
            #endif

            if (overrideComputeShader)
            {
                supportsComputeShaders = false;
            }
            if (supportsComputeShaders)
                InitializeCompute ();
            RegenerateSVTexture ();
        }
    }

    private void InitializeCompute()
    {
        if ( renderTexture == null )
        {
            renderTexture = new RenderTexture (textureWidth, textureHeight, 0, RenderTextureFormat.RGB111110Float);
            renderTexture.enableRandomWrite = true;
            renderTexture.Create ();
        }

        compute = Resources.Load<ComputeShader> ("Shaders/Compute/GenerateSVTexture");
        kernelID = compute.FindKernel ("CSMain");

        image.texture = renderTexture;
    }


    private void OnEnable()
    {
        if (Application.isPlaying && picker != null)
        {
            slider.onValueChanged.AddListener(SliderChanged);
            picker.onHSVChanged.AddListener(HSVChanged);
        }
    }

    private void OnDisable()
    {
        if (picker != null)
        {
            slider.onValueChanged.RemoveListener(SliderChanged);
            picker.onHSVChanged.RemoveListener(HSVChanged);
        }
    }

    private void OnDestroy()
    {
        if ( image.texture != null )
        {
            if ( supportsComputeShaders )
                renderTexture.Release ();
            else
                DestroyImmediate (image.texture);
        }
    }

    private void SliderChanged(float saturation, float value)
    {
        if (listen)
        {
            picker.AssignColor(ColorValues.Saturation, saturation);
            picker.AssignColor(ColorValues.Value, value);
        }
        listen = true;
    }

    private void HSVChanged(float h, float s, float v)
    {
        if (!lastH.Equals(h))
        {
            lastH = h;
            RegenerateSVTexture();
        }

        if (!s.Equals(slider.normalizedValue))
        {
            listen = false;
            slider.normalizedValue = s;
        }

        if (!v.Equals(slider.normalizedValueY))
        {
            listen = false;
            slider.normalizedValueY = v;
        }
    }

    private void RegenerateSVTexture()
    {
        if ( supportsComputeShaders )
        {
            float hue = picker != null ? picker.H : 0;

            compute.SetTexture (kernelID, "Texture", renderTexture);
            compute.SetFloats ("TextureSize", textureWidth, textureHeight);
            compute.SetFloat ("Hue", hue);

            compute.SetBool("linearColorSpace", QualitySettings.activeColorSpace == ColorSpace.Linear);

            var threadGroupsX = Mathf.CeilToInt (textureWidth / 32f);
            var threadGroupsY = Mathf.CeilToInt (textureHeight / 32f);
            compute.Dispatch (kernelID, threadGroupsX, threadGroupsY, 1);
        }
        else
        {
            double h = picker != null ? picker.H * 360 : 0;

            if ( image.texture != null )
                DestroyImmediate (image.texture);

            var texture = new Texture2D (textureWidth, textureHeight);
            texture.hideFlags = HideFlags.DontSave;

            for ( int s = 0; s < textureWidth; s++ )
            {
                Color32[] colors = new Color32[textureHeight];
                for ( int v = 0; v < textureHeight; v++ )
                {
                    colors[v] = HSVUtil.ConvertHsvToRgb (h, (float)s / 100, (float)v / 100, 1);
                }
                texture.SetPixels32 (s, 0, 1, textureHeight, colors);
            }
            texture.Apply ();

            image.texture = texture;
        }
    }
}
