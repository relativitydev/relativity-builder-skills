using System;
using kCura.EventHandler;

namespace MyCompany.MyApp.EventHandlers
{
    // Post Workspace Create: runs after a new workspace is created FROM A TEMPLATE WORKSPACE that
    // already has this application installed. It does not run for workspaces created any other way.
    // Attach it through the application's Application Event Handlers list.
    [kCura.EventHandler.CustomAttributes.Description("MyApp - New workspace setup")]
    [System.Runtime.InteropServices.Guid("00000000-0000-0000-0000-000000000000")]
    public class SamplePostWorkspaceCreateEventHandler : PostWorkspaceCreateEventHandlerBase
    {
        public override Response Execute()
        {
            var response = new Response { Success = true, Message = string.Empty };

            try
            {
                int newWorkspaceId = Helper.GetActiveCaseID();
                int templateWorkspaceId = TemplateWorkspaceID;
                // ... per-workspace setup ...
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
                response.Exception = ex;
            }

            return response;
        }
    }
}
