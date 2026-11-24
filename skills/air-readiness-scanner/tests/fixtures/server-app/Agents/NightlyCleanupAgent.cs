using System;
using System.Data;
using System.IO;
using kCura.Agent;
using kCura.Relativity.Client;
using Relativity.API;

namespace Contoso.Cleanup.Agents
{
    [kCura.Agent.CustomAttributes.Name("Nightly Cleanup Agent")]
    [System.Runtime.InteropServices.Guid("8f0d1c33-2a5e-4f5b-9a3e-0c6c2a9b1e11")]
    public class NightlyCleanupAgent : AgentBase
    {
        private const string ArchiveShare = @"\\fileserver01\RelativityArchive\";

        public override string Name => "Nightly Cleanup Agent";

        public override void Execute()
        {
            IDBContext workspaceDb = Helper.GetDBContext(1017123);
            DataTable rows = workspaceDb.ExecuteSqlStatementAsDataTable(
                "SELECT ArtifactID, ControlNumber FROM EDDSDBO.Document WHERE Reviewed = 0");

            workspaceDb.ExecuteNonQuerySQLStatement(
                "BEGIN TRAN; UPDATE EDDSDBO.Document SET Reviewed = 1; COMMIT");

            string processingSql = "SELECT * FROM [INV1017123].[dbo].[File] WHERE FileID > 0";
            workspaceDb.ExecuteSqlStatementAsDataTable(processingSql);

            foreach (string path in Directory.GetFiles(ArchiveShare))
            {
                File.Copy(path, Path.Combine(@"\\fileserver01\Export\", Path.GetFileName(path)));
            }

            using (IRSAPIClient client = Helper.GetServicesManager().CreateProxy<IRSAPIClient>(ExecutionIdentity.System))
            {
                client.APIOptions.WorkspaceID = 1017123;
            }

            RaiseMessage("Done", 10);
        }
    }
}
