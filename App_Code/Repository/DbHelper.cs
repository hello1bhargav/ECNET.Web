using System.Data;
using MySql.Data.MySqlClient;

public static class DbHelper
{
    private static string connStr = "server=localhost;user id=root;password=yourpassword;database=ecnet;";

    public static DataTable ExecuteQuery(string query, params MySqlParameter[] parameters)
    {
        using (MySqlConnection conn = new MySqlConnection(connStr))
        {
            MySqlCommand cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddRange(parameters);
            MySqlDataAdapter da = new MySqlDataAdapter(cmd);
            DataTable dt = new DataTable();
            da.Fill(dt);
            return dt;
        }
    }

    public static int ExecuteNonQuery(string query, params MySqlParameter[] parameters)
    {
        using (MySqlConnection conn = new MySqlConnection(connStr))
        {
            conn.Open();
            MySqlCommand cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddRange(parameters);
            return cmd.ExecuteNonQuery();
        }
    }
}
