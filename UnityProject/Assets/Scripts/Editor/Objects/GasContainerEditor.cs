using System.Linq;
using Atmospherics;
using Objects;
using UnityEditor;
using UnityEngine;
/*
[CustomEditor(typeof(GasContainer))]
public class GasContainerEditor : Editor
{
	private int selected;
	private bool showGasMix;

	private float[] ratios;

	private GasContainer container;

	private float pressure;

	private void OnEnable()
	{
		container = (GasContainer) target;

		container.UpdateGasMix();

		InitRatios();
	}

	private void InitRatios()
	{
		ratios = new float[Gas.Count];

		foreach (Gas gas in Gas.All)
		{
			ratios[gas] = container.GasMix.Moles > 0 ? container.Gases[gas] / container.GasMix.Moles : 0;
		}
	}

	public override void OnInspectorGUI()
	{
		base.OnInspectorGUI();

		container.Temperature = EditorGUILayout.FloatField("Temperature", container.Temperature);

		if (container.Temperature.Equals(float.NaN))
		{
			container.Temperature = 0;
		}


		EditorGUILayout.Space();

		selected = GUILayout.Toolbar(selected, new[] {"Absolute", "Ratios"});

		if (selected == 0)
		{
			AbsolutSelection();
		}
		else
		{
			RatioSelection();
		}

		container.UpdateGasMix();
		if (GUI.changed)
		{
			EditorUtility.SetDirty(container);
		}
	}

	private void AbsolutSelection()
	{
		EditorGUILayout.LabelField("Moles", $"{container.GasMix.Moles}");
		container.Gases = ShowGasValues(container.GasMix.Gases);

		pressure = AtmosUtils.CalcPressure(container.Volume, container.GasMix.Moles, container.Temperature);

		EditorGUILayout.LabelField("Pressure", $"{pressure}");
	}

	private void RatioSelection()
	{
		pressure = EditorGUILayout.FloatField("Pressure", container.GasMix.Pressure);

		float moles = AtmosUtils.CalcMoles(pressure, container.Volume, container.Temperature);

		ratios = ShowGasValues(ratios, "Ratios");

		float total = ratios.Sum();

		foreach (Gas gas in Gas.All)
		{
			container.Gases[gas] = total > 0 ? ratios[gas] / total * moles : 0;
		}
	}

	private float[] ShowGasValues(float[] values, string label = null)
	{
		float[] result = new float[Gas.Count];

		if (label != null)
		{
			EditorGUILayout.LabelField(label);
		}

		EditorGUI.indentLevel++;
		foreach (Gas gas in Gas.All)
		{
			result[gas] = EditorGUILayout.FloatField(gas.Name, values[gas]);
		}

		EditorGUI.indentLevel--;

		return result;
	}
}
*/

[CustomPropertyDrawer(typeof(GasMix))]
public class PointDrawer : PropertyDrawer
{
    SerializedProperty X, Y;
    string name;
    bool cache = false;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {

	    //get the name before it's gone
	    name = property.displayName;

	    //get the X and Y values
		var enumerator = property.GetEnumerator();
		while (enumerator.MoveNext())
		{
			Logger.Log((enumerator.Current as SerializedProperty).name);
		}

	    X = property.Copy();
	    property.Next(true);
	    Y = property.Copy();



        Rect contentPosition = EditorGUI.PrefixLabel(position, new GUIContent(name));

        //Check if there is enough space to put the name on the same line (to save space)
        if (position.height > 16f)
        {
            position.height = 16f;
            EditorGUI.indentLevel += 1;
            contentPosition = EditorGUI.IndentedRect(position);
            contentPosition.y += 18f;
        }

        float half = contentPosition.width / 2;
        GUI.skin.label.padding = new RectOffset(3, 3, 6, 6);

        //show the X and Y from the point
        EditorGUIUtility.labelWidth = 14f;
        contentPosition.width *= 0.5f;
        EditorGUI.indentLevel = 0;

        // Begin/end property & change check make each field
        // behave correctly when multi-object editing.
        EditorGUI.BeginProperty(contentPosition, label, X);
        {
            EditorGUI.BeginChangeCheck();
            //int newVal = EditorGUI.IntField(contentPosition, new GUIContent("X"), X.intValue);
            //if (EditorGUI.EndChangeCheck())
            //    X.intValue = newVal;
        }
        EditorGUI.EndProperty();

        contentPosition.x += half;

        EditorGUI.BeginProperty(contentPosition, label, Y);
        {
            EditorGUI.BeginChangeCheck();
            //int newVal = EditorGUI.IntField(contentPosition, new GUIContent("Y"), Y.intValue);
            //if (EditorGUI.EndChangeCheck())
            //    Y.intValue = newVal;
        }
        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return Screen.width < 333 ? (16f + 18f) : 16f;
    }
}