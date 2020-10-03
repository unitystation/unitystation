namespace UI.Items.PDA
{
	/// <summary>
	/// Allows the page to get ready before activation.
	/// </summary>
	interface IPageReadyable
	{
		/// <summary>
		/// Runs just before the page is activated.
		/// </summary>
		void OnPageActivated();
	}

	/// <summary>
	/// Allows the page to get ready before deactivation.
	/// </summary>
	interface IPageCleanupable
	{
		/// <summary>
		/// Runs just before the new page is activated.
		/// </summary>
		void OnPageDeactivated();
	}

	/// <summary>
	/// Allows the page to get ready before activation and deactivation.
	/// </summary>
	interface IPageLifecycle : IPageReadyable, IPageCleanupable { }
}
