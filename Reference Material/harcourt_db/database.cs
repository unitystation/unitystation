private SQLiteConnection sql_con;
private SQLiteCommand sql_cmd;
private SQLiteDataAdapter DB;
private DataSet DS = new DataSet();
private DataTable DT = new DataTable();

//To use:

private void SetConnectionUsers() 
{ 
sql_con = new SQLiteConnection
	("Data Source=users.db;Version=3;New=False;Compress=True;"); 
} 
private void SetConnectionLogs() 
{ 
sql_con = new SQLiteConnection
	("Data Source=logs.db;Version=3;New=False;Compress=True;"); 
} 

private void ExecuteQueryUsers(string txtQuery) 
{ 
SetConnectionUsers(); 
sql_con.Open(); 
sql_cmd = sql_con.CreateCommand(); 
sql_cmd.CommandText=txtQuery; 
sql_cmd.ExecuteNonQuery(); 
sql_con.Close(); 
}
private void ExecuteQueryLogs(string txtQuery) 
{ 
SetConnectionLogs(); 
sql_con.Open(); 
sql_cmd = sql_con.CreateCommand(); 
sql_cmd.CommandText=txtQuery; 
sql_cmd.ExecuteNonQuery(); 
sql_con.Close(); 
}

private void LoadUsers() 
{ 
SetConnectionUsers(); 
sql_con.Open(); 
sql_cmd = sql_con.CreateCommand(); 
//change to what you need here
string CommandText = "select id, ip from log"; 
DB = new SQLiteDataAdapter(CommandText,sql_con); 
DS.Reset(); 
DB.Fill(DS); 
DT= DS.Tables[0]; 
Grid.DataSource = DT; 
sql_con.Close(); 
}

private void LoadLogs() 
{ 
SetConnectionLogs(); 
sql_con.Open(); 
sql_cmd = sql_con.CreateCommand(); 
//change to what you need here
string CommandText = "select id, username from users"; 
DB = new SQLiteDataAdapter(CommandText,sql_con); 
DS.Reset(); 
DB.Fill(DS); 
DT= DS.Tables[0]; 
Grid.DataSource = DT; 
sql_con.Close(); 
}

private void AddUser()
{
string txtSQLQuery = "insert into users (steam_id, username, ip) values ('"+Id.Text+","username.Txt","ip.Txt"')";
ExecuteQuery(txtSQLQuery);            
}
private void AddLog()
{
string txtSQLQuery = "insert into  log (desc) values ('"+txtDesc.Text+"')";
ExecuteQuery(txtSQLQuery);            
}
