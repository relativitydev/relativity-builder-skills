using System;
using kCura.EventHandler;
using Relativity.API;
using Relativity.Services.Objects;
using Relativity.Services.Objects.DataContracts;

namespace MyCompany.MyApp.EventHandlers
{
    // Post Save: runs after a Document or RDO is written. The save has already happened, so
    // this cannot undo it. Use it for small follow-up work; queue anything slow for an agent.
    //
    // This sample writes a row to a separate "Activity Log" RDO. Avoid updating the object that
    // was just saved: an Object Manager update fires its Pre Save and Post Save handlers again.
    [kCura.EventHandler.CustomAttributes.Description("MyApp - Log Sample Object saves")]
    [System.Runtime.InteropServices.Guid("00000000-0000-0000-0000-000000000000")]
    public class SamplePostSaveEventHandler : PostSaveEventHandler
    {
        // GUIDs of the Activity Log object type and its Long Text "Message" field, from your application.
        private static readonly Guid ActivityLogObjectTypeGuid = new Guid("00000000-0000-0000-0000-000000000000");
        private static readonly Guid ActivityLogMessageFieldGuid = new Guid("00000000-0000-0000-0000-000000000000");

        public override Response Execute()
        {
            var response = new Response { Success = true, Message = string.Empty };
            IAPILog logger = Helper.GetLoggerFactory().GetLogger().ForContext<SamplePostSaveEventHandler>();

            try
            {
                int workspaceId = Helper.GetActiveCaseID();
                int artifactId = ActiveArtifact.ArtifactID;

                // System, because users who can edit the object may not have rights to the log.
                using (IObjectManager objectManager =
                    Helper.GetServicesManager().CreateProxy<IObjectManager>(ExecutionIdentity.System))
                {
                    var create = new CreateRequest
                    {
                        ObjectType = new ObjectTypeRef { Guid = ActivityLogObjectTypeGuid },
                        FieldValues = new[]
                        {
                            new FieldRefValuePair
                            {
                                Field = new FieldRef { Guid = ActivityLogMessageFieldGuid },
                                Value = $"ArtifactID {artifactId} saved by user {Helper.GetAuthenticationManager().UserInfo.ArtifactID} at {DateTime.UtcNow:u}",
                            },
                        },
                    };

                    // Execute() is synchronous, so block on the async call.
                    objectManager.CreateAsync(workspaceId, create).GetAwaiter().GetResult();
                }

                logger.LogInformation("Post Save logged ArtifactID {ArtifactID}.", artifactId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Post Save failed for ArtifactID {ArtifactID}.", ActiveArtifact.ArtifactID);
                response.Success = false;
                response.Message = "The record was saved, but follow-up processing failed. Contact your administrator.";
                response.Exception = ex;
            }

            return response;
        }

        public override FieldCollection RequiredFields => new FieldCollection();
    }
}
