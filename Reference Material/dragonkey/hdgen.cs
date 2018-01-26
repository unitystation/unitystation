using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.IO;

//Dragon Key
//Hard Dragon
namespace HWID
{
    class Program
    {
        public string HD()
        {
		OperatingSystem os = Environment.OSVersion;
		PlatformID     pid = os.Platform;
		case PlatformID.WinCE:
			return Value();
		case PlatformID.Unix:
			return GetUID();
		case PlatformID.MacOSX:
			return MacUUID();
		default:
			return 0

        }

        private static string _fingerPrint = string.Empty;
		
        private static string Value()
        {
                _fingerPrint = GetHash("CPU >> " + CpuId() + "\nBIOS >> " + BiosId() + "\nBASE >> " + BaseId() + "\nDISK >> " + DiskId() + "\nVIDEO >> " + VideoId() + "\nMAC >> " + MacId());
            return _fingerPrint;
        }
        private static string GetHash(string s)
        {
            //Initialize a new MD5 Crypto Service Provider in order to generate a hash
            MD5 sec = new MD5CryptoServiceProvider();
            //Grab the bytes of the variable 's'
            byte[] bt = Encoding.ASCII.GetBytes(s);
            //Grab the Hexadecimal value of the MD5 hash
            return GetHexString(sec.ComputeHash(bt));
        }

        private static string GetHexString(IList<byte> bt)
        {
            string s = string.Empty;
            for (int i = 0; i < bt.Count; i++)
            {
                byte b = bt[i];
                int n = b;
                int n1 = n & 15;
                int n2 = (n >> 4) & 15;
                if (n2 > 9)
                    s += ((char)(n2 - 10 + 'A')).ToString(CultureInfo.InvariantCulture);
                else
                    s += n2.ToString(CultureInfo.InvariantCulture);
                if (n1 > 9)
                    s += ((char)(n1 - 10 + 'A')).ToString(CultureInfo.InvariantCulture);
                else
                    s += n1.ToString(CultureInfo.InvariantCulture);
                if ((i + 1) != bt.Count && (i + 1) % 2 == 0) s += "-";
            }
            return s;
        }

        //Return a hardware identifier
        private static string Identifier(string wmiClass, string wmiProperty, string wmiMustBeTrue)
        {
            string result = "";
            System.Management.ManagementClass mc = new System.Management.ManagementClass(wmiClass);
            System.Management.ManagementObjectCollection moc = mc.GetInstances();
            foreach (System.Management.ManagementBaseObject mo in moc)
            {
                if (mo[wmiMustBeTrue].ToString() != "True") continue;
                //Only get the first one
                if (result != "") continue;
                try
                {
                    result = mo[wmiProperty].ToString();
                    break;
                }
                catch
                {
                }
            }
            return result;
        }
        //Return a hardware identifier
        private static string Identifier(string wmiClass, string wmiProperty)
        {
            string result = "";
            System.Management.ManagementClass mc = new System.Management.ManagementClass(wmiClass);
            System.Management.ManagementObjectCollection moc = mc.GetInstances();
            foreach (System.Management.ManagementBaseObject mo in moc)
            {
                //Only get the first one
                if (result != "") continue;
                try
                {
                    result = mo[wmiProperty].ToString();
                    break;
                }
                catch
                {
                }
            }
            return result;
        }
        private static string CpuId()
        {
            //Uses first CPU identifier available in order of preference
            //Don't get all identifiers, as it is very time consuming
            string retVal = Identifier("Win32_Processor", "UniqueId");
            if (retVal != "") return retVal;
            retVal = Identifier("Win32_Processor", "ProcessorId");
            if (retVal != "") return retVal;
            retVal = Identifier("Win32_Processor", "Name");
            if (retVal == "") //If no Name, use Manufacturer
            {
                retVal = Identifier("Win32_Processor", "Manufacturer");
            }
            //Add clock speed for extra security
            retVal += Identifier("Win32_Processor", "MaxClockSpeed");
            return retVal;
        }
        //BIOS Identifier
        private static string BiosId()
        {
            return Identifier("Win32_BIOS", "Manufacturer") + Identifier("Win32_BIOS", "SMBIOSBIOSVersion") + Identifier("Win32_BIOS", "IdentificationCode") + Identifier("Win32_BIOS", "SerialNumber") + Identifier("Win32_BIOS", "ReleaseDate") + Identifier("Win32_BIOS", "Version");
        }
        //Main physical hard drive ID
        private static string DiskId()
        {
            return Identifier("Win32_DiskDrive", "Model") + Identifier("Win32_DiskDrive", "Manufacturer") + Identifier("Win32_DiskDrive", "Signature") + Identifier("Win32_DiskDrive", "TotalHeads");
        }
        //Motherboard ID
        private static string BaseId()
        {
            return Identifier("Win32_BaseBoard", "Model") + Identifier("Win32_BaseBoard", "Manufacturer") + Identifier("Win32_BaseBoard", "Name") + Identifier("Win32_BaseBoard", "SerialNumber");
        }
        //Primary video controller ID
        private static string VideoId()
        {
            return Identifier("Win32_VideoController", "DriverVersion") + Identifier("Win32_VideoController", "Name");
        }
        //First enabled network card ID
        private static string MacId()
        {
            return Identifier("Win32_NetworkAdapterConfiguration", "MACAddress", "IPEnabled");
        }
    }
	private static string MacUUID ()
	{
		var startInfo = new ProcessStartInfo () {
			FileName = "sh",
			Arguments = "-c \"ioreg -rd1 -c IOPlatformExpertDevice | awk '/IOPlatformUUID/'\"",
			UseShellExecute = false,
			CreateNoWindow = true,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			RedirectStandardInput = true,
			UserName = Environment.UserName
		};
		var builder = new StringBuilder ();
		using (Process process = Process.Start (startInfo)) {
			process.WaitForExit ();
			builder.Append (process.StandardOutput.ReadToEnd ());
		}
		string str = builder.ToString;
		// Get the integral value of the character.
		int value = Convert.ToInt32(str);
		// Convert the decimal value to a hexadecimal value in string form.
		string hexOutput = String.Format(value);
		string input = hexOutput;
		StringBuilder sb = new StringBuilder();
		for (int i = 0; i < input.Length; i++)
		{
			if (i % 3 == 0)
				sb.Append('-');
			sb.Append(input[i]);
		}
		string formatted = sb.ToString();
		return(formatted);
	}
	private string GetUID()
	{
		StringBuilder strB = new StringBuilder();
		Guid G = new Guid(); HidD_GetHidGuid(ref G);
		strB.Append(Convert.ToString(G));
		IntPtr lHWInfoPtr = Marshal.AllocHGlobal(123); HWProfile lProfile = new HWProfile();
		Marshal.StructureToPtr(lProfile, lHWInfoPtr, false);
		if (GetCurrentHwProfile(lHWInfoPtr))
		{
			Marshal.PtrToStructure(lHWInfoPtr, lProfile);
			strB.Append(lProfile.szHwProfileGuid.Trim(new char[] { '{', '}' }));
		}
		Marshal.FreeHGlobal(lHWInfoPtr);
		SHA256CryptoServiceProvider SHA256 = new SHA256CryptoServiceProvider();
		byte[] B = Encoding.Default.GetBytes(strB.ToString());
		string outStr = BitConverter.ToString(SHA256.ComputeHash(B)).Repla ce("-", null);
		for(int i = 0;i < 64; i++)
		{
			if (i % 16 == 0 && i != 0) 
				outStr = outStr.Insert(i, "-");
		} 

		return (outStr);
	}
	[DllImport("hid.dll")]
	private static extern void HidD_GetHidGuid(ref Guid GUID);
	[DllImport("advapi32.dll", SetLastError = true)]
	static extern bool GetCurrentHwProfile(IntPtr fProfile);
	[StructLayout(LayoutKind.Sequential)]
	class HWProfile
	{
		public Int32 dwDockInfo;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 39)]
		public string szHwProfileGuid;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
		public string szHwProfileName;
	}
}
