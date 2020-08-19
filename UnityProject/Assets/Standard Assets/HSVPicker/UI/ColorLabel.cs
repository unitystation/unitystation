using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class ColorLabel : MonoBehaviour
{
    public ColorPicker picker;

    public ColorValues type;

    public string prefix = "R: ";
    public float minValue = 0;
    public float maxValue = 255;

    public int precision = 0;

	private InputField inputField;
    private void Awake()
    {
		inputField = GetComponentInParent<InputField>();
		inputField.characterValidation = InputField.CharacterValidation.Integer;
    }

    private void OnEnable()
    {
        if (Application.isPlaying && picker != null)
        {
			picker.onValueChanged.AddListener(ColorChanged);
            picker.onHSVChanged.AddListener(HSVChanged);
        }
		inputField.onEndEdit.AddListener(UpdateColor);
	}

	private void OnDestroy()
    {
        if (picker != null)
        {
            picker.onValueChanged.RemoveListener(ColorChanged);
            picker.onHSVChanged.RemoveListener(HSVChanged);
        }
		inputField.onEndEdit.RemoveListener(UpdateColor);
	}

    private void ColorChanged(Color color)
    {
		UpdateValue();
    }

    private void HSVChanged(float hue, float sateration, float value)
    {
		UpdateValue();
    }

    private void UpdateValue()
    {
        if (picker == null)
        {
			inputField.text = prefix + "-";
        }
        else
        {
            float value = minValue + (picker.GetValue(type) * (maxValue - minValue));
			inputField.text = prefix + ConvertToDisplayString(value);
		}
	}
	private string ConvertToDisplayString(float value)
	{
		if (precision > 0)
			return value.ToString("f " + precision);
		else
			return Mathf.FloorToInt(value).ToString();
	}
	private void UpdateColor(string newRGB)
	{
		float value = ConvertInputValueToRGBFloat(newRGB);
		switch (type)
		{
			case ColorValues.R:
				picker.R = value;
				break;
			case ColorValues.G:
				picker.G = value;
				break;
			case ColorValues.B:
				picker.B = value;
				break;
			default:
				break;
		}
	}

	private float ConvertInputValueToRGBFloat(string newRGB)
	{
		float value = float.Parse(newRGB);
		if(value > maxValue)
			value = maxValue;
		return value / maxValue;
	}
}
