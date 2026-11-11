using System;
using kCura.EventHandler;
using kCura.EventHandler.CustomAttributes;

namespace MyCompany.MyApp.EventHandlers
{
    // Post Install: runs after the application installs or upgrades in a workspace.
    // Use it for setup the application's schema cannot express, such as seeding default records.
    // Success = false fails the install and shows Message on the install status page.
    //
    // RunOnce(true): runs once per successful install; if the install fails it runs again on the
    //   next attempt. Use RunOnce(false) only for work that must repeat on every install/upgrade.
    // RunTarget: where it runs. Workspace = each workspace the app is installed into;
    //   Instance = once at the instance (admin) level; InstanceAndWorkspace = both.
    // Make Execute idempotent either way: it may run again after a failed install.
    // Attach install handlers through the application's "Install Event Handlers" list, not an object type.
    [Description("MyApp - Post install setup")]
    [System.Runtime.InteropServices.Guid("00000000-0000-0000-0000-000000000000")]
    [RunOnce(true)]
    [RunTarget(kCura.EventHandler.Helper.RunTargets.Workspace)]
    public class SamplePostInstallEventHandler : PostInstallEventHandler
    {
        public override Response Execute()
        {
            var response = new Response { Success = true, Message = string.Empty };

            try
            {
                int workspaceId = Helper.GetActiveCaseID();

                // CurrentVersion is the previously installed version (null on first install);
                // VersionInstalling is the version being installed.
                bool isFirstInstall = CurrentVersion == null;

                Helper.GetLoggerFactory().GetLogger().ForContext<SamplePostInstallEventHandler>()
                    .LogInformation("Post install in workspace {WorkspaceID}; first install: {IsFirstInstall}; version {Version}.",
                        workspaceId, isFirstInstall, VersionInstalling);
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "MyApp setup failed: " + ex.Message;
                response.Exception = ex;
            }

            return response;
        }
    }
}
