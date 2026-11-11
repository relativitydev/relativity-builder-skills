using System;
using kCura.EventHandler;
using Relativity.API;

namespace MyCompany.MyApp.EventHandlers
{
    // Pre Cascade Delete: runs before an object that has dependent objects is force-deleted.
    // It is the delete handler that can BLOCK the delete: set Success = false AND set Exception.
    // (Returning Success = false from a Pre Delete handler does not stop the delete.)
    //
    // The Artifact IDs being deleted are in the SQL table named by
    // TempTableNameWithParentArtifactsToDelete. Commit() is never called for this handler type.
    [kCura.EventHandler.CustomAttributes.Description("MyApp - Block delete of locked Sample Objects")]
    [System.Runtime.InteropServices.Guid("00000000-0000-0000-0000-000000000000")]
    public class SamplePreCascadeDeleteEventHandler : PreCascadeDeleteEventHandler
    {
        public override Response Execute()
        {
            var response = new Response { Success = true, Message = string.Empty };

            try
            {
                if (AnyLockedObjectsInDeleteSet())
                {
                    const string message = "One or more selected objects are locked and cannot be deleted.";
                    response.Success = false;
                    response.Message = message;
                    response.Exception = new InvalidOperationException(message);
                }
            }
            catch (Exception ex)
            {
                Helper.GetLoggerFactory().GetLogger().ForContext<SamplePreCascadeDeleteEventHandler>()
                    .LogError(ex, "Pre Cascade Delete check failed.");
                response.Success = false;
                response.Message = "The delete check could not be completed.";
                response.Exception = ex;
            }

            return response;
        }

        private bool AnyLockedObjectsInDeleteSet()
        {
            // Replace with your rule. TempTableNameWithParentArtifactsToDelete holds the IDs being
            // deleted; prefer Object Manager queries over direct SQL where you can.
            return false;
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
