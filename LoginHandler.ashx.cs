using System;
using System.Web;
using System.Web.Script.Serialization;
using System.Data;
using System.Data.Odbc;
using System.Configuration;

namespace ECNET.Web
{
    public class LoginHandler : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            // Set CORS headers to allow cross-origin requests
            context.Response.AddHeader("Access-Control-Allow-Origin", "*");
            context.Response.AddHeader("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
            context.Response.AddHeader("Access-Control-Allow-Headers", "Content-Type");

            context.Response.ContentType = "application/json";
            string responseJson = "";
            var jsSerializer = new JavaScriptSerializer();

            // Handle OPTIONS request for CORS preflight
            if (context.Request.HttpMethod == "OPTIONS")
            {
                context.Response.StatusCode = 200;
                return;
            }

            if (context.Request.HttpMethod == "POST")
            {
                try
                {
                    // Read the incoming JSON request body
                    string requestBody = new System.IO.StreamReader(context.Request.InputStream).ReadToEnd();

                    // Debug log the request body
                    System.Diagnostics.Debug.WriteLine("Request Body: " + requestBody);

                    if (string.IsNullOrEmpty(requestBody))
                    {
                        context.Response.StatusCode = 400;
                        responseJson = jsSerializer.Serialize(new { Message = "Empty request body" });
                        context.Response.Write(responseJson);
                        return;
                    }

                    dynamic loginData = jsSerializer.Deserialize<dynamic>(requestBody);

                    // Extract username, password, and role from the deserialized data
                    string username = loginData["username"];
                    string password = loginData["password"];
                    string role = loginData["role"];

                    // Validate input
                    if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(role))
                    {
                        context.Response.StatusCode = 400;
                        responseJson = jsSerializer.Serialize(new { Message = "Username, password, and role are required" });
                        context.Response.Write(responseJson);
                        return;
                    }

                    // Query the database to authenticate the user
                    string query = "SELECT UserId, Username, Role FROM Users WHERE Username = ? AND Password = ? AND Role = ?";
                    OdbcParameter[] parameters = new OdbcParameter[]
                    {
                        new OdbcParameter("username", username),
                        new OdbcParameter("password", password),
                        new OdbcParameter("role", role)
                    };

                    // Execute the query using DbHelper
                    DataTable dt = DbHelper.ExecuteQuery(query, parameters);

                    if (dt.Rows.Count > 0)
                    {
                        // User found and authenticated
                        var user = new User
                        {
                            UserId = Convert.ToInt32(dt.Rows[0]["UserId"]),
                            Username = dt.Rows[0]["Username"].ToString(),
                            Role = dt.Rows[0]["Role"].ToString()
                        };
                        responseJson = jsSerializer.Serialize(user);
                        context.Response.StatusCode = 200;
                    }
                    else
                    {
                        // Authentication failed
                        context.Response.StatusCode = 401;
                        responseJson = jsSerializer.Serialize(new { Message = "Invalid credentials or role mismatch." });
                    }
                }
                catch (Exception ex)
                {
                    // Handle any exceptions during processing
                    context.Response.StatusCode = 500;
                    responseJson = jsSerializer.Serialize(new { Message = "Server error: " + ex.Message });

                    // Debug log the exception
                    System.Diagnostics.Debug.WriteLine("Exception: " + ex.ToString());
                }
            }
            else
            {
                // If not a POST request, return Method Not Allowed
                context.Response.StatusCode = 405;
                responseJson = jsSerializer.Serialize(new { Message = "Only POST requests are allowed for login." });
            }

            context.Response.Write(responseJson);
        }

        public bool IsReusable
        {
            get { return false; }
        }
    }
}