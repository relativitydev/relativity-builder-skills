using System;
using System.Data.SqlClient;
using System.Web.UI;

namespace Contoso.Cleanup.CustomPage
{
    public partial class Default : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Session["lastRun"] = DateTime.UtcNow;

            // Old approach, kept for reference:
            // var ctx = Relativity.CustomPages.ConnectionHelper.Helper().GetDBContext(-1);

            using (var conn = new SqlConnection("Server=sql01;Database=EDDS;Integrated Security=true"))
            {
                conn.Open();
                var cmd = new SqlCommand("SELECT TOP 10 * FROM EDDSDBO.AuditRecord_PrimaryPartition ORDER BY ID DESC", conn);
                cmd.ExecuteReader();
            }
        }
    }
}
