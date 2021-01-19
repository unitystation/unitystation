using UnityEngine;

public class ColorPickerTester : MonoBehaviour 
{

    public new Renderer renderer;
    public ColorPicker picker;

    public Color Color = Color.red;

	// Use this for initialization
	void Start () 
    {
        picker.onValueChanged.AddListener(color =>
        {
            renderer.material.color = color;
            Color = color;
        });

		renderer.material.color = picker.CurrentColor;

        picker.CurrentColor = Color;
    }
	
	// Update is called once per frame
	void Update () {
	
	}
}
