using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestVariableViewerScript : MonoBehaviour
{
	public bool Pbool = true;
	public int Pint = 55;
	public string pstring = "yoyyyoy";
	private string PVstring = "My porn collection"; //Me private variables!!!
	public Teststruct pTeststruct;
	public Connection pConnection  = Connection.Overlap;
	public List<int> PListInt = new List<int>();
	public List<bool> PListbool = new List<bool>();
	public List<string> PListstring = new List<string>();
	public List<Teststruct> PListTeststruct = new List<Teststruct>();
	public List<Connection> PListConnection = new List<Connection>();

	public HashSet<int> PHashSetInt = new HashSet<int>();
	public HashSet<bool> PHashSetbool = new HashSet<bool>();
	public HashSet<string> PHashSetstring = new HashSet<string>();
	public HashSet<Connection> PHashSetConnection = new HashSet<Connection>();
	public HashSet<object> PHashSetobject = new HashSet<object>();

	public Dictionary<int,int> PDictionaryIntInt = new Dictionary<int, int>();
	public Dictionary<bool,bool> PDictionaryboolbool  = new Dictionary<bool, bool>();
	public Dictionary<string, string> PDictionarystringstring  = new Dictionary<string, string>();
	public Dictionary<Connection, Connection> PDictionaryConnectionConnection = new Dictionary<Connection, Connection>();

	public Dictionary<string, HashSet<int>> DictionaryHashSet = new Dictionary<string, HashSet<int>>();
	public Dictionary<string, List<int>> DictionaryList = new Dictionary<string, List<int>>();

	public int length = 10;


	void Start()
    {
        for (int i = 0; i < length; i++)
		{
			PListInt.Add(i);
			PListbool.Add(true);
			PListstring.Add(i.ToString() + "< t");
			PListConnection.Add(Connection.East);
			var GG = new Teststruct
			{
				author = ("BOB" + i),
				price = i,
				title = i + "COOL?"
			};
			pTeststruct = GG;
			PListTeststruct.Add(GG);
			PHashSetInt.Add(i);
			PHashSetbool.Add(true);
			PHashSetstring.Add(i.ToString() + "< t");
			PHashSetConnection.Add(Connection.East);

			PDictionaryIntInt[i] = i;
			PDictionaryboolbool[true] = true;
			PDictionarystringstring[i.ToString()] = "titymm";
			PDictionaryConnectionConnection[Connection.MachineConnect] = Connection.East;

			DictionaryHashSet[i.ToString()] = PHashSetInt;
			DictionaryList[i.ToString()] = PListInt;
		}
    }
}


public struct Teststruct
{
	public decimal price;
	public string title;
	public string author;
}