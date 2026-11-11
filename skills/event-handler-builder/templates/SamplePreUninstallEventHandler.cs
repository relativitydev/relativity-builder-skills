using System;
using kCura.EventHandler;
using kCura.EventHandler.CustomAttributes;

namespace MyCompany.MyApp.EventHandlers
{
    // Pre Uninstall: runs before the application is removed from a workspace. Use it to clean up
    // anything the application created outside its own schema. Return Success = false with a clear Message if clean-up fails.
    [Description("MyApp - Pre uninstall clean-up")]
    [System.Runtime.InteropServices.Guid("00000000-0000-0000-0000-000000000000")]
    [RunTarget(kCura.EventHandler.Helper.RunTargets.Workspace)]
    public class SamplePreUninstallEventHandler : PreUninstallEventHandler
    {
        public override Response Execute()
        {
            var response = new Response { Success = true, Message = string.Empty };

            try
            {
                int workspaceId = Helper.GetActiveCaseID();
                // ... clean up ...
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "MyApp clean-up failed: " + ex.Message;
                response.Exception = ex;
            }

            return response;
        }
    }
}
