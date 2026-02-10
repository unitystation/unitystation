namespace US13.Variable_Viewer
{
	public interface ISelectionGizmo
	{
		public void OnSelected();
		public void OnDeselect();
		public void UpdateGizmos();
	}
}
