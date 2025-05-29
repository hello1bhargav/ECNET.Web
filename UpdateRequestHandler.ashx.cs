using System;
using System.Web;
using System.Web.Script.Serialization;
using System.Data;
using System.Data.Odbc; // Changed from MySql.Data.MySqlClient
using System.Collections.Generic; // Required for List<T>

// Make sure your DbHelper and Request classes are accessible within the ECNET.Web namespace.

namespace ECNET.Web
{
    public class UpdateRequestHandler : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "application/json";
            string responseJson = "";
            var jsSerializer = new JavaScriptSerializer();

            if (context.Request.HttpMethod == "POST")
            {
                try
                {
                    string requestBody = new System.IO.StreamReader(context.Request.InputStream).ReadToEnd();
                    dynamic updateData = jsSerializer.Deserialize<dynamic>(requestBody);

                    int requestId = updateData["Id"];
                    string newStatus = updateData["Status"];
                    // Fix for Error 9, 11, 14: Use 'as string' or null-coalescing for potential null values
                    string hodComments = updateData["HodComments"] as string;
                    string itChampionComments = updateData["ItChampionComments"] as string;
                    string workflowStage = updateData["WorkflowStage"];
                    string assignedHOD = updateData["AssignedHOD"] as string;


                    string query = "UPDATE Requests SET Status = ?, WorkflowStage = ?"; // ODBC uses '?'
                    List<OdbcParameter> parameters = new List<OdbcParameter>(); // Changed from MySqlParameter
                    parameters.Add(new OdbcParameter("Status", newStatus));
                    parameters.Add(new OdbcParameter("WorkflowStage", workflowStage));

                    // Conditionally add parameters based on who is updating
                    if (!string.IsNullOrEmpty(hodComments)) // Check for actual string content
                    {
                        query += ", HodComments = ?";
                        parameters.Add(new OdbcParameter("HodComments", hodComments));
                    }
                    if (!string.IsNullOrEmpty(itChampionComments)) // Check for actual string content
                    {
                        query += ", ItChampionComments = ?";
                        parameters.Add(new OdbcParameter("ItChampionComments", itChampionComments));
                        query += ", ItChampionReviewDate = ?"; // Set review date for IT Champion
                        parameters.Add(new OdbcParameter("ItChampionReviewDate", DateTime.Now));
                    }
                    if (!string.IsNullOrEmpty(assignedHOD)) // Check for actual string content
                    {
                        query += ", AssignedHOD = ?"; // Assuming a column for assigned HOD in Requests table
                        parameters.Add(new OdbcParameter("AssignedHOD", assignedHOD));
                    }

                    query += " WHERE Id = ?"; // ODBC uses '?'
                    parameters.Add(new OdbcParameter("Id", requestId));

                    int rowsAffected = DbHelper.ExecuteNonQuery(query, parameters.ToArray()); // DbHelper now expects OdbcParameter

                    if (rowsAffected > 0)
                    {
                        responseJson = jsSerializer.Serialize(new { Message = "Request updated successfully." });
                        context.Response.StatusCode = 200;
                    }
                    else
                    {
                        context.Response.StatusCode = 404; // Not Found or No rows affected
                        responseJson = jsSerializer.Serialize(new { Message = "Request not found or no changes made." });
                    }
                }
                catch (Exception ex)
                {
                    context.Response.StatusCode = 500;
                    responseJson = jsSerializer.Serialize(new { Message = "Server error: " + ex.Message });
                }
            }
            else
            {
                context.Response.StatusCode = 405; // Method Not Allowed
                responseJson = jsSerializer.Serialize(new { Message = "Only POST requests are allowed for updating requests." });
            }

            context.Response.Write(responseJson);
        }

        public bool IsReusable
        {
            get { return false; }
        }
    }
}