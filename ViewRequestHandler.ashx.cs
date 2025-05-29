using System;
using System.Web;
using System.Web.Script.Serialization;
using System.Data;
using System.Data.Odbc; // Changed from MySql.Data.MySqlClient
using System.Collections.Generic; // Required for List<T>

// Make sure your DbHelper and Request classes are accessible within the ECNET.Web namespace.

namespace ECNET.Web
{
    public class ViewRequestsHandler : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "application/json";
            string responseJson = "";
            var jsSerializer = new JavaScriptSerializer();

            if (context.Request.HttpMethod == "GET")
            {
                try
                {
                    // Determine the type of requests to fetch based on query parameters
                    string roleFilter = context.Request.QueryString["role"]; // e.g., "hod", "it-champion"
                    string statusFilter = context.Request.QueryString["status"]; // e.g., "Pending", "Approved"
                    string applicantNameFilter = context.Request.QueryString["applicant"]; // e.g., for IT Champion to filter by applicant

                    string query = "SELECT * FROM Requests WHERE 1=1"; // Start with a always-true condition
                    List<OdbcParameter> parameters = new List<OdbcParameter>(); // Changed from MySqlParameter

                    if (!string.IsNullOrEmpty(roleFilter))
                    {
                        if (roleFilter.ToLower() == "hod")
                        {
                            // HOD might see all pending requests or requests from their department
                            // In a real app, you'd filter by HOD's department (e.g., query += " AND Department = ?").
                            // For now, it will fetch all requests if no other filters apply.
                        }
                        else if (roleFilter.ToLower() == "it-champion")
                        {
                            // IT Champion might see requests assigned to them or all pending requests
                            // Example: query += " AND ItChampionName = ?";
                            // parameters.Add(new OdbcParameter("itChampionName", "champion1")); // Placeholder
                        }
                    }

                    if (!string.IsNullOrEmpty(statusFilter))
                    {
                        query += " AND Status = ?"; // ODBC uses '?'
                        parameters.Add(new OdbcParameter("statusFilter", statusFilter));
                    }

                    if (!string.IsNullOrEmpty(applicantNameFilter))
                    {
                        query += " AND ApplicantName = ?"; // ODBC uses '?'
                        parameters.Add(new OdbcParameter("applicantNameFilter", applicantNameFilter));
                    }

                    query += " ORDER BY RequestDate DESC"; // Order by most recent requests

                    DataTable dt = DbHelper.ExecuteQuery(query, parameters.ToArray()); // DbHelper now expects OdbcParameter

                    List<Request> requests = new List<Request>();
                    foreach (DataRow row in dt.Rows)
                    {
                        requests.Add(new Request
                        {
                            Id = Convert.ToInt32(row["Id"]),
                            ApplicantName = row["ApplicantName"].ToString(),
                            Department = row["Department"].ToString(),
                            Designation = row["Designation"].ToString(),
                            Email = row["Email"].ToString(),
                            ContactPhone = row["ContactPhone"].ToString(),
                            SystemMake = row["SystemMake"].ToString(),
                            Configuration = row["Configuration"].ToString(),
                            OsName = row["OsName"].ToString(),
                            AntivirusName = row["AntivirusName"] != DBNull.Value ? row["AntivirusName"].ToString() : null,
                            MacAddress = row["MacAddress"].ToString(),
                            ItChampionName = row["ItChampionName"] != DBNull.Value ? row["ItChampionName"].ToString() : null,
                            RequestDate = Convert.ToDateTime(row["RequestDate"]),
                            Status = row["Status"].ToString(),
                            HodComments = row["HodComments"] != DBNull.Value ? row["HodComments"].ToString() : null,
                            WorkflowStage = row["WorkflowStage"].ToString(),
                            // Fix for Error 28: Explicitly cast to DateTime? if the source can be null
                            ItChampionReviewDate = row["ItChampionReviewDate"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(row["ItChampionReviewDate"]) : null
                        });
                    }

                    responseJson = jsSerializer.Serialize(requests);
                    context.Response.StatusCode = 200;
                }
                catch (Exception ex)
                {
                    context.Response.StatusCode = 500;
                    responseJson = jsSerializer.Serialize(new { Message = "Server error: " + ex.Message });
                }
            }
            else
            {
                context.Response.StatusCode = 405;
                responseJson = jsSerializer.Serialize(new { Message = "Only GET requests are allowed for viewing requests." });
            }

            context.Response.Write(responseJson);
        }

        public bool IsReusable
        {
            get { return false; }
        }
    }
}