using System;
using System.Web;
using System.Web.Script.Serialization;
using System.Data;
using System.Data.Odbc;
using System.Collections.Generic;
using System.IO;

namespace ECNET.Web
{
    public class ViewRequestHandler : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "application/json";
            context.Response.AddHeader("Access-Control-Allow-Origin", "*");

            string responseJson = "";
            var jsSerializer = new JavaScriptSerializer();

            if (context.Request.HttpMethod == "GET")
            {
                try
                {
                    // Get filter parameters from query string
                    string roleFilter = context.Request.QueryString["role"];
                    string statusFilter = context.Request.QueryString["status"];
                    string applicantNameFilter = context.Request.QueryString["applicant"];
                    string departmentFilter = context.Request.QueryString["department"];
                    string workflowStageFilter = context.Request.QueryString["workflowStage"];

                    // Get pagination parameters
                    int page = 1;
                    int pageSize = 50;

                    if (!string.IsNullOrEmpty(context.Request.QueryString["page"]))
                        int.TryParse(context.Request.QueryString["page"], out page);

                    if (!string.IsNullOrEmpty(context.Request.QueryString["pageSize"]))
                        int.TryParse(context.Request.QueryString["pageSize"], out pageSize);

                    // Build query with filters
                    string query = "SELECT * FROM Requests WHERE 1=1";
                    List<OdbcParameter> parameters = new List<OdbcParameter>();

                    // Apply role-based filtering
                    if (!string.IsNullOrEmpty(roleFilter))
                    {
                        if (roleFilter.ToLower() == "hod")
                        {
                            // HOD should only see requests that reached the HOD stage
                            query += " AND WorkflowStage = ?";
                            parameters.Add(new OdbcParameter("stage", "HOD Review"));
                        }
                        else if (roleFilter.ToLower() == "it" || roleFilter.ToLower() == "it-champion")
                        {
                            // IT Champion sees only requests that are assigned to them
                            query += " AND WorkflowStage = ?";
                            parameters.Add(new OdbcParameter("stage", "IT Champion Review"));
                        }
                    }


                    // Apply other filters
                    if (!string.IsNullOrEmpty(statusFilter))
                    {
                        query += " AND Status = ?";
                        parameters.Add(new OdbcParameter("status", statusFilter));
                    }

                    if (!string.IsNullOrEmpty(applicantNameFilter))
                    {
                        query += " AND ApplicantName LIKE ?";
                        parameters.Add(new OdbcParameter("applicant", "%" + applicantNameFilter + "%"));
                    }

                    if (!string.IsNullOrEmpty(departmentFilter))
                    {
                        query += " AND Department = ?";
                        parameters.Add(new OdbcParameter("department", departmentFilter));
                    }

                    if (!string.IsNullOrEmpty(workflowStageFilter))
                    {
                        query += " AND WorkflowStage = ?";
                        parameters.Add(new OdbcParameter("workflowStage", workflowStageFilter));
                    }

                    // Add ordering
                    query += " ORDER BY RequestDate DESC";

                    // Execute query
                    DataTable dt = DbHelper.ExecuteQuery(query, parameters.ToArray());

                    List<ViewRequestModel> requests = new List<ViewRequestModel>();
                    foreach (DataRow row in dt.Rows)
                    {
                        requests.Add(new ViewRequestModel
                        {
                            Id = Convert.ToInt32(row["Id"]),
                            RequestID = Convert.ToInt32(row["Id"]), // Add this for JavaScript compatibility
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
                            ItChampionReviewDate = row["ItChampionReviewDate"] != DBNull.Value ?
                                (DateTime?)Convert.ToDateTime(row["ItChampionReviewDate"]) : null
                        });
                    }

                    var response = new
                    {
                        Requests = requests,
                        CurrentPage = page,
                        PageSize = pageSize,
                        TotalRecords = requests.Count,
                        FilterApplied = new
                        {
                            Role = roleFilter,
                            Status = statusFilter,
                            Applicant = applicantNameFilter,
                            Department = departmentFilter,
                            WorkflowStage = workflowStageFilter
                        }
                    };

                    responseJson = jsSerializer.Serialize(response);
                    context.Response.StatusCode = 200;
                }
                catch (Exception ex)
                {
                    context.Response.StatusCode = 500;
                    responseJson = jsSerializer.Serialize(new
                    {
                        Message = "Server error: " + ex.Message,
                        StackTrace = ex.StackTrace // Remove in production
                    });
                }
            }
            else if (context.Request.HttpMethod == "POST")
            {
                try
                {
                    string requestBody = "";
                    using (StreamReader reader = new StreamReader(context.Request.InputStream))
                    {
                        requestBody = reader.ReadToEnd();
                    }

                    if (string.IsNullOrEmpty(requestBody))
                    {
                        context.Response.StatusCode = 400;
                        responseJson = jsSerializer.Serialize(new { Message = "Request body is empty." });
                        context.Response.Write(responseJson);
                        return;
                    }

                    dynamic updateData = jsSerializer.Deserialize<dynamic>(requestBody);

                    if (updateData == null)
                    {
                        context.Response.StatusCode = 400;
                        responseJson = jsSerializer.Serialize(new { Message = "Invalid JSON data." });
                        context.Response.Write(responseJson);
                        return;
                    }

                    int requestId = 0;
                    string action = null;
                    string hodComments = "";

                    // Handle both Id and requestId for compatibility
                    if (updateData.ContainsKey("Id"))
                        int.TryParse(updateData["Id"].ToString(), out requestId);
                    else if (updateData.ContainsKey("requestId"))
                        int.TryParse(updateData["requestId"].ToString(), out requestId);

                    if (updateData.ContainsKey("Action"))
                        action = updateData["Action"] != null ? updateData["Action"].ToString() : null;
                    else if (updateData.ContainsKey("action"))
                        action = updateData["action"] != null ? updateData["action"].ToString() : null;

                    if (updateData.ContainsKey("HodComments"))
                        hodComments = updateData["HodComments"] != null ? updateData["HodComments"].ToString() : "";
                    else
                        hodComments = "";

                    if (requestId == 0)
                    {
                        context.Response.StatusCode = 400;
                        responseJson = jsSerializer.Serialize(new { Message = "Invalid or missing request ID." });
                        context.Response.Write(responseJson);
                        return;
                    }

                    if (string.IsNullOrEmpty(action))
                    {
                        context.Response.StatusCode = 400;
                        responseJson = jsSerializer.Serialize(new { Message = "Action is required." });
                        context.Response.Write(responseJson);
                        return;
                    }

                    string updateQuery = "";
                    List<OdbcParameter> updateParameters = new List<OdbcParameter>();

                    switch (action.ToLower())
                    {
                        case "hod_approve":
                            updateQuery = "UPDATE Requests SET Status = ?, WorkflowStage = ?, HodComments = ? WHERE Id = ?";
                            updateParameters.Add(new OdbcParameter("status", "HOD Approved"));
                            updateParameters.Add(new OdbcParameter("stage", "IT Champion Review"));
                            updateParameters.Add(new OdbcParameter("comments", hodComments));
                            updateParameters.Add(new OdbcParameter("id", requestId));
                            break;

                        case "hod_reject":
                            updateQuery = "UPDATE Requests SET Status = ?, WorkflowStage = ?, HodComments = ? WHERE Id = ?";
                            updateParameters.Add(new OdbcParameter("status", "HOD Rejected"));
                            updateParameters.Add(new OdbcParameter("stage", "Rejected"));
                            updateParameters.Add(new OdbcParameter("comments", hodComments));
                            updateParameters.Add(new OdbcParameter("id", requestId));
                            break;

                        case "it_complete":
                        case "forward_to_hod":
                            updateQuery = "UPDATE Requests SET Status = ?, WorkflowStage = ?, ItChampionReviewDate = ? WHERE Id = ?";
                            updateParameters.Add(new OdbcParameter("status", "Pending HOD Review"));
                            updateParameters.Add(new OdbcParameter("stage", "HOD Review"));
                            updateParameters.Add(new OdbcParameter("reviewDate", DateTime.Now));
                            updateParameters.Add(new OdbcParameter("id", requestId));
                            break;

                        default:
                            context.Response.StatusCode = 400;
                            responseJson = jsSerializer.Serialize(new { Message = "Invalid action specified: " + action });
                            context.Response.Write(responseJson);
                            return;
                    }

                    int affectedRows = DbHelper.ExecuteNonQuery(updateQuery, updateParameters.ToArray());

                    if (affectedRows > 0)
                    {
                        responseJson = jsSerializer.Serialize(new
                        {
                            success = true,
                            Message = "Request updated successfully.",
                            AffectedRows = affectedRows,
                            RequestId = requestId,
                            Action = action
                        });
                        context.Response.StatusCode = 200;
                    }
                    else
                    {
                        responseJson = jsSerializer.Serialize(new
                        {
                            success = false,
                            Message = "No rows were updated. Request ID may not exist or no changes were made.",
                            RequestId = requestId
                        });
                        context.Response.StatusCode = 400;
                    }
                }
                catch (Exception ex)
                {
                    context.Response.StatusCode = 500;
                    responseJson = jsSerializer.Serialize(new
                    {
                        success = false,
                        Message = "Server error: " + ex.Message,
                        StackTrace = ex.StackTrace // Remove in production
                    });
                }
            }
            else
            {
                context.Response.StatusCode = 405;
                responseJson = jsSerializer.Serialize(new { Message = "Only GET and POST requests are allowed." });
            }

            context.Response.Write(responseJson);
        }

        public bool IsReusable
        {
            get { return false; }
        }
    }

    public class ViewRequestModel
    {
        public int Id { get; set; }
        public int RequestID { get; set; } // Add this for JavaScript compatibility
        public string ApplicantName { get; set; }
        public string Department { get; set; }
        public string Designation { get; set; }
        public string Email { get; set; }
        public string ContactPhone { get; set; }
        public string SystemMake { get; set; }
        public string Configuration { get; set; }
        public string OsName { get; set; }
        public string AntivirusName { get; set; }
        public string MacAddress { get; set; }
        public string ItChampionName { get; set; }
        public DateTime RequestDate { get; set; }
        public string Status { get; set; }
        public string HodComments { get; set; }
        public string WorkflowStage { get; set; }
        public DateTime? ItChampionReviewDate { get; set; }
    }
}