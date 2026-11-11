using System;
using kCura.EventHandler;

namespace MyCompany.MyApp.EventHandlers
{
    // Pre Mass Delete: runs once before a set of objects is deleted, for one-time work that
    // covers the whole set (Pre Delete still runs per object). The Artifact IDs are in the SQL
    // table named by TempTableNameWithParentArtifactsToDelete.
    // Commit() and Rollback() are called around Relativity's delete transaction.
    [kCura.EventHandler.CustomAttributes.Description("MyApp - Prepare Sample Object mass delete")]
    [System.Runtime.InteropServices.Guid("00000000-0000-0000-0000-000000000000")]
    public class SamplePreMassDeleteEventHandler : PreMassDeleteEventHandler
    {
        public override Response Execute()
        {
            var response = new Response { Success = true, Message = string.Empty };

            try
            {
                string idTable = TempTableNameWithParentArtifactsToDelete;
                Helper.GetLoggerFactory().GetLogger().ForContext<SamplePreMassDeleteEventHandler>()
                    .LogInformation("Mass delete starting; IDs are in {IdTable}.", idTable);
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
                response.Exception = ex;
            }

            return response;
        }

        public override void Commit()
        {
        }

        public override void Rollback()
        {
        }

        public override FieldCollection RequiredFields => new FieldCollection();
    }
}
