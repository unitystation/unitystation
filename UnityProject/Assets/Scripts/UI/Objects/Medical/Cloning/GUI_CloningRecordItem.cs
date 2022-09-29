using Objects.Medical;
using UI.Core.NetUI;

namespace UI.Objects.Medical.Cloning
{
	public class GUI_CloningRecordItem : DynamicEntry
	{
		public CloningRecord cloningRecord;
		public GUI_Cloning gui_Cloning;

		public NetText_label recordName;
		public NetText_label recrodScanID;

		public void SetValues()
		{
			recordName.MasterSetValue(cloningRecord.name);
			recrodScanID.MasterSetValue("Scan ID " + cloningRecord.scanID);
		}

		public void ViewRecord()
		{
			gui_Cloning.ViewRecord(cloningRecord);
		}
	}
}
