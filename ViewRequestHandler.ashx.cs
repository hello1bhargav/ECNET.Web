using System;
using System.Web;
using System.Web.Script.Serialization;
using System.Data;
using System.Data.Odbc;
using System.Collections.Generic;

namespace ECNET.Web
{
    public class ViewRequestHandler : IHttpHandler // Renamed class here
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
                    // Get query parameters for filtering
                    string roleFilter = context.Request.QueryString["role"]; // "hod", "it-champion", "admin"
                    string statusFilter = context.Request.QueryString["status"]; // "Pending", "Approved", "Rejected"
                    string applicantNameFilter = context.Request.QueryString["applicant"];
                    string departmentFilter = context.Request.QueryString["department"];
                    string workflowStageFilter = context.Request.QueryString["stage"]; // "Submitted", "HOD Review", "IT Champion Review", "Completed"
                    string requestIdFilter = context.Request.QueryString["id"]; // For viewing specific request

                    // Pagination parameters
                    int page = 1;
                    int pageSize = 50;

                    if (!string.IsNullOrEmpty(context.Request.QueryString["page"]))
                        int.TryParse(context.Request.QueryString["page"], out page);

                    if (!string.IsNullOrEmpty(context.Request.QueryString["pageSize"]))
                        int.TryParse(context.Request.QueryString["pageSize"], out pageSize);

                    string query = "SELECT * FROM Requests WHERE 1=1";
                    List<OdbcParameter> parameters = new List<OdbcParameter>();

                    // Apply role-based filtering
                    if (!string.IsNullOrEmpty(roleFilter))
                    {
                        switch (roleFilter.ToLower())
                        {
                            case "hod":
                                // HOD sees requests from their department that need approval
                                // You might want to add department matching logic here
                                query += " AND (WorkflowStage = 'Submitted' OR WorkflowStage = 'HOD Review')";
                                break;

                            case "it-champion":
                                // IT Champion sees requests assigned to them or ready for IT review
                                query += " AND (WorkflowStage = 'IT Champion Review' OR WorkflowStage = 'Completed')";
                                // Optionally filter by specific IT Champion
                                // query += " AND ItChampionName = ?";
                                // parameters.Add(new OdbcParameter("itChampionName", "currentChampionName"));
                                break;

                            case "admin":
                                // Admin sees all requests - no additional filter needed
                                break;
                        }
                    }

                    // Apply status filter
                    if (!string.IsNullOrEmpty(statusFilter))
                    {
                        query += " AND Status = ?";
                        parameters.Add(new OdbcParameter("statusFilter", statusFilter));
                    }

                    // Apply applicant name filter
                    if (!string.IsNullOrEmpty(applicantNameFilter))
                    {
                        query += " AND ApplicantName LIKE ?";
                        parameters.Add(new OdbcParameter("applicantNameFilter", "%" + applicantNameFilter + "%"));
                    }

                    // Apply department filter
                    if (!string.IsNullOrEmpty(departmentFilter))
                    {
                        query += " AND Department = ?";
                        parameters.Add(new OdbcParameter("departmentFilter", departmentFilter));
                    }

                    // Apply workflow stage filter
                    if (!string.IsNullOrEmpty(workflowStageFilter))
                    {
                        query += " AND WorkflowStage = ?";
                        parameters.Add(new OdbcParameter("workflowStageFilter", workflowStageFilter));
                    }

                    // Apply specific request ID filter
                    if (!string.IsNullOrEmpty(requestIdFilter))
                    {
                        query += " AND Id = ?";
                        parameters.Add(new OdbcParameter("requestIdFilter", requestIdFilter));
                    }

                    // Add ordering and pagination
                    query += " ORDER BY RequestDate DESC";

                    // Add pagination (adjust syntax based on your database)
                    // For MySQL: LIMIT offset, count
                    // For SQL Server: OFFSET offset ROWS FETCH NEXT count ROWS ONLY
                    int offset = (page - 1) * pageSize;
                    query += " LIMIT " + offset.ToString() + ", " + pageSize.ToString();

                    DataTable dt = DbHelper.ExecuteQuery(query, parameters.ToArray());

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
                            ItChampionReviewDate = row["ItChampionReviewDate"] != DBNull.Value ?
                                (DateTime?)Convert.ToDateTime(row["ItChampionReviewDate"]) : null
                        });
                    }

                    // Get total count for pagination (optional)
                    string countQuery = "SELECT COUNT(*) FROM Requests WHERE 1=1";
                    // Apply same filters to count query (you can reuse the same WHERE conditions)

                    var response = new
                    {
                        Requests = requests,
                        CurrentPage = page,
                        PageSize = pageSize,
                        TotalRecords = requests.Count, // You might want to get actual total from count query
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
                    responseJson = jsSerializer.Serialize(new { Message = "Server error: " + ex.Message });
                }
            }
            else if (context.Request.HttpMethod == "POST")
            {
                // Handle request updates (HOD comments, status changes, etc.)
                try
                {
                    string requestBody = new System.IO.StreamReader(context.Request.InputStream).ReadToEnd();
                    dynamic updateData = jsSerializer.Deserialize<dynamic>(requestBody);

                    int requestId = Convert.ToInt32(updateData["Id"]);
                    string action = updateData["Action"] != null ? updateData["Action"].ToString() : null; // "approve", "reject", "update_comments"

                    string updateQuery = "";
                    List<OdbcParameter> updateParameters = new List<OdbcParameter>();

                    if (action != null)
                    {
                        switch (action.ToLower())
                        {
                            case "hod_approve":
                                updateQuery = "UPDATE Requests SET Status = ?, WorkflowStage = ?, HodComments = ? WHERE Id = ?";
                                updateParameters.Add(new OdbcParameter("status", "HOD Approved"));
                                updateParameters.Add(new OdbcParameter("stage", "IT Champion Review"));
                                updateParameters.Add(new OdbcParameter("comments", updateData["HodComments"] != null ? updateData["HodComments"].ToString() : ""));
                                updateParameters.Add(new OdbcParameter("id", requestId));
                                break;

                            case "hod_reject":
                                updateQuery = "UPDATE Requests SET Status = ?, WorkflowStage = ?, HodComments = ? WHERE Id = ?";
                                updateParameters.Add(new OdbcParameter("status", "HOD Rejected"));
                                updateParameters.Add(new OdbcParameter("stage", "Rejected"));
                                updateParameters.Add(new OdbcParameter("comments", updateData["HodComments"] != null ? updateData["HodComments"].ToString() : ""));
                                updateParameters.Add(new OdbcParameter("id", requestId));
                                break;

                            case "it_complete":
                                updateQuery = "UPDATE Requests SET Status = ?, WorkflowStage = ?, ItChampionReviewDate = ? WHERE Id = ?";
                                updateParameters.Add(new OdbcParameter("status", "Completed"));
                                updateParameters.Add(new OdbcParameter("stage", "Completed"));
                                updateParameters.Add(new OdbcParameter("reviewDate", DateTime.Now));
                                updateParameters.Add(new OdbcParameter("id", requestId));
                                break;

                            default:
                                context.Response.StatusCode = 400;
                                responseJson = jsSerializer.Serialize(new { Message = "Invalid action specified." });
                                context.Response.Write(responseJson);
                                return;
                        }
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                        responseJson = jsSerializer.Serialize(new { Message = "Action is required." });
                        context.Response.Write(responseJson);
                        return;
                    }

                    DbHelper.ExecuteNonQuery(updateQuery, updateParameters.ToArray());

                    responseJson = jsSerializer.Serialize(new { Message = "Request updated successfully." });
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
                responseJson = jsSerializer.Serialize(new { Message = "Only GET and POST requests are allowed." });
            }

            context.Response.Write(responseJson);
        }

        public bool IsReusable
        {
            get { return false; }
        }
    }
}