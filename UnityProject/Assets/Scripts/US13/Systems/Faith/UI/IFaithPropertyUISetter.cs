namespace US13.Systems.Faith.UI
{
	public interface IFaithPropertyUISetter
	{
		public string UnfocusedText { get; set; }
		public void SetDesc(string desc);
	}
}