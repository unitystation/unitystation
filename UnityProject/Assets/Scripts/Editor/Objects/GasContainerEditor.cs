using System.Linq;
using Atmospherics;
using UnityEditor;
using UnityEngine;
using Objects.GasContainer;

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

		foreach (Gas gas in Gas.All)
		{
			result[gas] = EditorGUILayout.FloatField(gas.Name, values[gas]);
		}


		return result;
	}
}


