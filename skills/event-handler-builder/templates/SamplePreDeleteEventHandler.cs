using System;
using kCura.EventHandler;
using Relativity.API;

namespace MyCompany.MyApp.EventHandlers
{
    // Pre Delete: runs before an object is deleted, including deletes from a mass delete.
    // Use it for clean-up that must happen before the row disappears, such as removing related
    // records your application owns. It CANNOT block the delete: Success = false is ignored.
    // To block deletes, use a Pre Cascade Delete handler.
    //
    // Commit() and Rollback() are called around Relativity's delete transaction, so you can
    // finalize or undo your own work to match the outcome.
    // ActiveArtifact.Fields is not populated for this handler type.
    [kCura.EventHandler.CustomAttributes.Description("MyApp - Clean up before Sample Object delete")]
    [System.Runtime.InteropServices.Guid("00000000-0000-0000-0000-000000000000")]
    public class SamplePreDeleteEventHandler : PreDeleteEventHandler
    {
        public override Response Execute()
        {
            var response = new Response { Success = true, Message = string.Empty };

            try
            {
                int artifactId = ActiveArtifact.ArtifactID;
                // ... prepare clean-up for artifactId ...
            }
            catch (Exception ex)
            {
                Helper.GetLoggerFactory().GetLogger().ForContext<SamplePreDeleteEventHandler>()
                    .LogError(ex, "Pre Delete failed for ArtifactID {ArtifactID}.", ActiveArtifact.ArtifactID);
                response.Success = false;
                response.Message = ex.Message;
                response.Exception = ex;
            }

            return response;
        }

        public override void Commit()
        {
            // The delete succeeded: finalize the clean-up started in Execute().
        }

        public override void Rollback()
        {
            // The delete failed: undo anything Execute() changed.
        }

        public override FieldCollection RequiredFields => new FieldCollection();
    }
}
