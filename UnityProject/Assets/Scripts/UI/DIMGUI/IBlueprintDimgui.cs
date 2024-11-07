using UImGui;

namespace UI.DIMGUI
{
	public interface IBlueprintDimgui
	{
		public void Setup()
		{
			UImGuiUtility.Layout += OnLayout;
			UImGuiUtility.OnInitialize += OnCreateLayout;
			UImGuiUtility.OnDeinitialize += OnStopLayout;
		}

		public void Kill()
		{
			UImGuiUtility.Layout -= OnLayout;
			UImGuiUtility.OnInitialize -= OnCreateLayout;
			UImGuiUtility.OnDeinitialize -= OnStopLayout;
		}

		public void OnCreateLayout(UImGui.UImGui obj);
		public void OnLayout(UImGui.UImGui obj);
		public void OnStopLayout(UImGui.UImGui obj);
	}
}