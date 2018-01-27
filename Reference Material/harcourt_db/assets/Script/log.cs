using System.ComponentModel;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Mono.Data.Sqlite;
using System.Data;
using System;



public class insert : MonoBehaviour {
	private string conn, sqlQuery;
	IDbConnection dbconn;
	IDbCommand dbcmd;
	// Use this for initialization
	void Start () {
		conn = "URI=file:logs.db"; //Path to database.
		insertvalue("Harcourt","thisismysteamid","IP","HD","SD","ND");
		readers ();

	}


	private void insertvalue(string player_name, string SteamID, string IP, string HD, string SD, string ND)
	{
		using (dbconn = new SqliteConnection(conn))
		{
			dbconn.Open(); //Open connection to the database.
			dbcmd = dbconn.CreateCommand();
			sqlQuery = string.Format("insert into log (player_name, SteamID, IP, HD, SD, ND) values (\"{0}\",\"{1}\",\"{2}\",\"{3}\",\"{4}\",\"{5}\",\"{6}\")",player_name,SteamID,IP,HD,SD,ND);
			dbcmd.CommandText = sqlQuery;
			dbcmd.ExecuteScalar();
			dbconn.Close();
		}
	}
	private void readers()
	{
		using (dbconn = new SqliteConnection(conn))
		{
			dbconn.Open(); //Open connection to the database.
			dbcmd = dbconn.CreateCommand();
			sqlQuery = "SELECT * " + "FROM log";// table name
			dbcmd.CommandText = sqlQuery;
			IDataReader reader = dbcmd.ExecuteReader();
			Debug.Log(reader);
			Console.WriteLine (reader);
			reader.Close();
			reader = null;
			dbcmd.Dispose();
			dbcmd = null;
			dbconn.Close();
			dbconn = null;
		}
	}

	// Update is called once per frame
	void Update () {

	}
}
