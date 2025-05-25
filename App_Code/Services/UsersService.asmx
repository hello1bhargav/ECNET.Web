<%@ WebService Language="C#" Class="UsersService" %>
using System;
using System.Data;
using System.Web.Services;
using MySql.Data.MySqlClient;

[WebService(Namespace = "http://localhost:5024/api/Users")]
public class UsersService : WebService
{
    [WebMethod]
    public object Login(string username, string password)
    {
        string query = "SELECT * FROM Users WHERE Username=@username AND Password=@password";
        var dt = DbHelper.ExecuteQuery(query,
            new MySqlParameter("@username", username),
            new MySqlParameter("@password", password)
        );

        if (dt.Rows.Count == 1)
        {
            var row = dt.Rows[0];
            return new {
                Success = true,
                Username = row["Username"].ToString(),
                Role = row["Role"].ToString(),
                UserId = Convert.ToInt32(row["UserId"])
            };
        }

        Context.Response.StatusCode = 401;
        return new { Message = "Invalid credentials" };
    }
}
