[WebMethod]
public object SubmitRequest(Request req)
{
    string query = @"INSERT INTO Requests 
        (ApplicantName, Department, Designation, Email, ContactPhone, SystemMake, Configuration, 
        OsName, AntivirusName, MacAddress, ItChampionName, RequestDate, Status)
        VALUES 
        (@ApplicantName, @Department, @Designation, @Email, @ContactPhone, @SystemMake, @Configuration,
        @OsName, @AntivirusName, @MacAddress, @ItChampionName, @RequestDate, 'Pending')";

    int rows = DbHelper.ExecuteNonQuery(query,
        new MySqlParameter("@ApplicantName", req.ApplicantName),
        new MySqlParameter("@Department", req.Department),
        new MySqlParameter("@Designation", req.Designation),
        new MySqlParameter("@Email", req.Email),
        new MySqlParameter("@ContactPhone", req.ContactPhone),
        new MySqlParameter("@SystemMake", req.SystemMake),
        new MySqlParameter("@Configuration", req.Configuration),
        new MySqlParameter("@OsName", req.OsName),
        new MySqlParameter("@AntivirusName", req.AntivirusName),
        new MySqlParameter("@MacAddress", req.MacAddress),
        new MySqlParameter("@ItChampionName", req.ItChampionName),
        new MySqlParameter("@RequestDate", DateTime.Now)
    );

    return new { Success = rows > 0 };
}
