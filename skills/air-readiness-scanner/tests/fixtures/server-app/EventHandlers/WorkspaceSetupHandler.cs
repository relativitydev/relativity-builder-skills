using System;
using System.Threading.Tasks;
using kCura.EventHandler;
using Relativity.API;
using Relativity.Environment.V1.Workspace;
using Relativity.Environment.V1.Workspace.Models;

namespace Contoso.Cleanup.EventHandlers
{
    [kCura.EventHandler.CustomAttributes.Description("Adds a console with a cleanup button")]
    [System.Runtime.InteropServices.Guid("1d7e7c52-1a3f-4b69-8d3a-6f1c2b9d0a21")]
    public class CleanupConsole : ConsoleEventHandler
    {
        public override kCura.EventHandler.Console GetConsole(PageEvent pageEvent)
        {
            return new kCura.EventHandler.Console { Title = "Cleanup" };
        }

        public override void OnButtonClick(ConsoleButton consoleButton) { }

        public override FieldCollection RequiredFields => new FieldCollection();
    }

    public class WorkspaceCloner
    {
        private readonly IHelper _helper;

        public WorkspaceCloner(IHelper helper) { _helper = helper; }

        public async Task CloneAsync(WorkspaceRequest request)
        {
            using (var manager = _helper.GetServicesManager().CreateProxy<IWorkspaceManager>(ExecutionIdentity.System))
            {
                var created = await manager.CreateAsync(request);
                // Configure the new workspace right away.
                request.Name = created.Name + " (configured)";
                await manager.UpdateAsync(created.ArtifactID, request);
            }
        }
    }
}
