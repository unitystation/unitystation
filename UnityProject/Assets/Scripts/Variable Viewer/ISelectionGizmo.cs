using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ISelectionGizmo
{
	public void OnSelected();
	public void OnDeselect();
	public void UpdateGizmos();
}
