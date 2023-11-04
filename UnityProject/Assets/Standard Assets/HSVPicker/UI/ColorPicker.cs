using System;
using Assets.HSVPicker;
using UnityEngine;

public class ColorPicker : MonoBehaviour
{

    private float _hue = 0;
    private float _saturation = 0;
    private float _brightness = 0;

    private float _red = 1;
    private float _green = 0;
    private float _blue = 0;

    private float _alpha = 1;

    [Header("Setup")]
    public ColorPickerSetup Setup;

    [Header("Event")]
    public ColorChangedEvent onValueChanged = new ColorChangedEvent();
    public ColorChangedEvent onColourApply = new ColorChangedEvent();


    public HSVChangedEvent onHSVChanged = new HSVChangedEvent();

    public Action<Color> DynamicValueChangeEvent;

	private Color lastColor;
    public Color CurrentColor
    {
        get
        {
            return new Color(_red, _green, _blue, _alpha);
        }
        set
        {
            if (CurrentColor == value)
                return;

            _red = value.r;
            _green = value.g;
            _blue = value.b;
            _alpha = value.a;

            RGBChanged();

            SendChangedEvent();
        }
    }

    private void Start()
    {
        Setup.AlphaSlidiers.Toggle(Setup.ShowAlpha);
        Setup.ColorToggleElement.Toggle(Setup.ShowColorSliderToggle);
        Setup.RgbSliders.Toggle(Setup.ShowRgb);
        Setup.HsvSliders.Toggle(Setup.ShowHsv);
        Setup.ColorBox.Toggle(Setup.ShowColorBox);

        HandleHeaderSetting(Setup.ShowHeader);
        UpdateColorToggleText();

        RGBChanged();
        SendChangedEvent();
    }

	private void OnEnable()
	{
		// save color before edit
		lastColor = CurrentColor;
	}

	private void OnDisable()
	{
		DynamicValueChangeEvent = null;
	}

	public float H
    {
        get
        {
            return _hue;
        }
        set
        {
            if (_hue == value)
                return;

            _hue = value;

            HSVChanged();

            SendChangedEvent();
        }
    }

    public float S
    {
        get
        {
            return _saturation;
        }
        set
        {
            if (_saturation == value)
                return;

            _saturation = value;

            HSVChanged();

            SendChangedEvent();
        }
    }

    public float V
    {
        get
        {
            return _brightness;
        }
        set
        {
            if (_brightness == value)
                return;

            _brightness = value;

            HSVChanged();

            SendChangedEvent();
        }
    }

    public float R
    {
        get
        {
            return _red;
        }
        set
        {
            if (_red == value)
                return;

            _red = value;

            RGBChanged();

            SendChangedEvent();
        }
    }

    public float G
    {
        get
        {
            return _green;
        }
        set
        {
            if (_green == value)
                return;

            _green = value;

            RGBChanged();

            SendChangedEvent();
        }
    }

    public float B
    {
        get
        {
            return _blue;
        }
        set
        {
            if (_blue == value)
                return;

            _blue = value;

            RGBChanged();

            SendChangedEvent();
        }
    }

    private float A
    {
        get
        {
            return _alpha;
        }
        set
        {
            if (_alpha == value)
                return;

            _alpha = value;

            SendChangedEvent();
        }
    }

    public void EnablePicker(Action<Color> onChange)
    {
	    DynamicValueChangeEvent += onChange;
	    gameObject.SetActive(true);
    }

    private void RGBChanged()
    {
        HsvColor color = HSVUtil.ConvertRgbToHsv(CurrentColor);

        _hue = color.normalizedH;
        _saturation = color.normalizedS;
        _brightness = color.normalizedV;
    }

    private void HSVChanged()
    {
        Color color = HSVUtil.ConvertHsvToRgb(_hue * 360, _saturation, _brightness, _alpha);

        _red = color.r;
        _green = color.g;
        _blue = color.b;
    }

    private void SendChangedEvent()
    {
        onValueChanged?.Invoke(CurrentColor);
        onHSVChanged?.Invoke(_hue, _saturation, _brightness);
        DynamicValueChangeEvent?.Invoke(CurrentColor);
    }

    public void AssignColor(ColorValues type, float value)
    {
        switch (type)
        {
            case ColorValues.R:
                R = value;
                break;
            case ColorValues.G:
                G = value;
                break;
            case ColorValues.B:
                B = value;
                break;
            case ColorValues.A:
                A = value;
                break;
            case ColorValues.Hue:
                H = value;
                break;
            case ColorValues.Saturation:
                S = value;
                break;
            case ColorValues.Value:
                V = value;
                break;
            default:
                break;
        }
    }

    public void AssignColor(Color color)
    {
        CurrentColor = color;
    }

    public float GetValue(ColorValues type)
    {
        switch (type)
        {
            case ColorValues.R:
                return R;
            case ColorValues.G:
                return G;
            case ColorValues.B:
                return B;
            case ColorValues.A:
                return A;
            case ColorValues.Hue:
                return H;
            case ColorValues.Saturation:
                return S;
            case ColorValues.Value:
                return V;
            default:
                throw new System.NotImplementedException("");
        }
    }

    public void ToggleColorSliders()
    {
        Setup.ShowHsv = !Setup.ShowHsv;
        Setup.ShowRgb = !Setup.ShowRgb;
        Setup.HsvSliders.Toggle(Setup.ShowHsv);
        Setup.RgbSliders.Toggle(Setup.ShowRgb);


        UpdateColorToggleText();
    }

    void UpdateColorToggleText()
    {
        if (Setup.ShowRgb)
        {
            Setup.SliderToggleButtonText.text = "RGB";
        }

        if (Setup.ShowHsv)
        {
            Setup.SliderToggleButtonText.text = "HSV";
        }
    }

    private void HandleHeaderSetting(ColorPickerSetup.ColorHeaderShowing setupShowHeader)
    {
        if (setupShowHeader == ColorPickerSetup.ColorHeaderShowing.Hide)
        {
            Setup.ColorHeader.Toggle(false);
            return;
        }

        Setup.ColorHeader.Toggle(true);

        Setup.ColorPreview.Toggle(setupShowHeader != ColorPickerSetup.ColorHeaderShowing.ShowColorCode);
        Setup.ColorCode.Toggle(setupShowHeader != ColorPickerSetup.ColorHeaderShowing.ShowColor);

    }

	public void OnApplyBtn()
	{
		onColourApply.Invoke(CurrentColor);
		// close with currentcolor
		gameObject.SetActive(false);
	}

	public void OnCancelBtn()
	{
		// return to previous color and close
		CurrentColor = lastColor;
		gameObject.SetActive(false);
	}
}
