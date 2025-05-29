using System.Data;
using System.Data.Odbc;
using System.Configuration;

public static class DbHelper
{
    private static string connStr = ConfigurationManager.ConnectionStrings["EcnetOdbcConnection"].ConnectionString;

    public static DataTable ExecuteQuery(string query, params OdbcParameter[] parameters)
    {
        using (OdbcConnection conn = new OdbcConnection(connStr))
        {
            OdbcCommand cmd = new OdbcCommand(query, conn);
            if (parameters != null && parameters.Length > 0)
            {
                cmd.Parameters.AddRange(parameters);
            }

            OdbcDataAdapter da = new OdbcDataAdapter(cmd);
            DataTable dt = new DataTable();
            da.Fill(dt);
            return dt;
        }
    }

    public static int ExecuteNonQuery(string query, params OdbcParameter[] parameters)
    {
        using (OdbcConnection conn = new OdbcConnection(connStr))
        {
            conn.Open();
            OdbcCommand cmd = new OdbcCommand(query, conn);
            if (parameters != null && parameters.Length > 0)
            {
                cmd.Parameters.AddRange(parameters);
            }

            return cmd.ExecuteNonQuery();
        }
    }
}
