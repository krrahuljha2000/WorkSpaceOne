
static void InsertF5Row()
{
	string connectionString = "Server=128.64.0.10;Database=website_airwatchse;User=sa;Password=P@ssw0rd;";
	string queryString = "INSERT INTO wrkshopf5queue (wrkshop_id, wrkshopaction_id, wrkshopuser_id, vapp_id) VALUES ([WRKSHOP_ID], 1,[WRKSHOPUSER_ID], '[VAPP_ID]')";
	
	try
	{
		SqlConnection dbConn = new SqlConnection();
		SqlCommand dbCmd = new SqlCommand();
		SqlDataAdapter dbSqlAdapter = new SqlDataAdapter(dbCmd);

		dbConn.ConnectionString = connectionString;
		dbConn.Open();
		dbCmd.Connection = dbConn;
		dbCmd.CommandText = queryString;

		dbConn.Close();
	}
	catch (Exception ex)
	{ }
	finally 
	{
		dbConn.Close();
	}
}