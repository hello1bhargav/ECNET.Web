using System; // Fix for Error 4, 5, 19, 20: Add using System; for DateTime

namespace ECNET.Web // It's good practice to put models in a namespace
{
    public class Request
    {
        public int Id { get; set; }
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
        public string Status { get; set; } // Pending, Approved, Declined, etc.
        public string HodComments { get; set; }
        public string WorkflowStage { get; set; }
        public DateTime? ItChampionReviewDate { get; set; }
    }
}