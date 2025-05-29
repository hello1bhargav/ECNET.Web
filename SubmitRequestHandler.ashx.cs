using System;
using System.Web;
using System.Web.Script.Serialization;
using System.Data;
using System.Data.Odbc;

namespace ECNET.Web
{
    public class SubmitRequestHandler : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "application/json";
            context.Response.AddHeader("Access-Control-Allow-Origin", "*");

            string responseJson = "";
            var jsSerializer = new JavaScriptSerializer();

            if (context.Request.HttpMethod == "POST")
            {
                try
                {
                    string requestBody = new System.IO.StreamReader(context.Request.InputStream).ReadToEnd();
                    dynamic requestData = jsSerializer.Deserialize<dynamic>(requestBody);

                    dynamic details = requestData["Details"];

                    var newRequest = new Request
                    {
                        ApplicantName = details["applicantName"],
                        Department = details["divSecCode"],
                        Designation = details["designation"],
                        ContactPhone = details["contactPhone"],
                        Email = details["email"],
                        SystemMake = details["systemMake"],
                        Configuration = details["configuration"],
                        OsName = details["osName"],
                        AntivirusName = details["antivirusName"],
                        MacAddress = details["macAddress"],
                        ItChampionName = details["itChampion"],
                        RequestDate = DateTime.Now,
                        Status = "Pending",
                        WorkflowStage = "Submitted",
                        HodComments = null,
                        ItChampionReviewDate = null
                    };

                    // 1. INSERT request (no SELECT here)
                    string insertQuery = @"INSERT INTO Requests (ApplicantName, Department, Designation, Email, ContactPhone,
                                         SystemMake, Configuration, OsName, AntivirusName, MacAddress, ItChampionName,
                                         RequestDate, Status, WorkflowStage, HodComments, ItChampionReviewDate)
                                 VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";

                    OdbcParameter[] parameters = new OdbcParameter[]
                    {
                        new OdbcParameter("ApplicantName", newRequest.ApplicantName),
                        new OdbcParameter("Department", newRequest.Department),
                        new OdbcParameter("Designation", newRequest.Designation),
                        new OdbcParameter("Email", newRequest.Email),
                        new OdbcParameter("ContactPhone", newRequest.ContactPhone),
                        new OdbcParameter("SystemMake", newRequest.SystemMake),
                        new OdbcParameter("Configuration", newRequest.Configuration),
                        new OdbcParameter("OsName", newRequest.OsName),
                        new OdbcParameter("AntivirusName", (object)newRequest.AntivirusName ?? DBNull.Value),
                        new OdbcParameter("MacAddress", newRequest.MacAddress),
                        new OdbcParameter("ItChampionName", (object)newRequest.ItChampionName ?? DBNull.Value),
                        new OdbcParameter("RequestDate", newRequest.RequestDate),
                        new OdbcParameter("Status", newRequest.Status),
                        new OdbcParameter("WorkflowStage", newRequest.WorkflowStage),
                        new OdbcParameter("HodComments", (object)newRequest.HodComments ?? DBNull.Value),
                        new OdbcParameter("ItChampionReviewDate", (object)newRequest.ItChampionReviewDate ?? DBNull.Value)
                    };

                    // Execute insert (no result expected)
                    DbHelper.ExecuteNonQuery(insertQuery, parameters);

                    // 2. Get last inserted ID (in separate query)
                    string idQuery = "SELECT LAST_INSERT_ID();";
                    DataTable idResult = DbHelper.ExecuteQuery(idQuery, null);
                    int newRequestId = Convert.ToInt32(idResult.Rows[0][0]);

                    // Success response
                    responseJson = jsSerializer.Serialize(new { Id = newRequestId, Message = "Request submitted successfully." });
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
                responseJson = jsSerializer.Serialize(new { Message = "Only POST requests are allowed for submitting requests." });
            }

            context.Response.Write(responseJson);
        }

        public bool IsReusable
        {
            get { return false; }
        }
    }
}
