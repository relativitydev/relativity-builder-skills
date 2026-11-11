using System;
using System.Linq;
using System.Threading.Tasks;
using MyCompany.MyApp.Interfaces.MyModule.v1;
using MyCompany.MyApp.Interfaces.MyModule.v1.Exceptions;
using MyCompany.MyApp.Interfaces.MyModule.v1.Models;
using Relativity.API;
using Relativity.Kepler.Logging;
using Relativity.Services.Objects;
using Relativity.Services.Objects.DataContracts;
using Relativity.Services.Exceptions;

namespace MyCompany.MyApp.Services.MyModule.v1
{
    // Exactly one implementation per service interface, and every method must be public.
    // Kepler's dependency injection supplies IHelper and ILog on each call.
    public class SampleService : ISampleService
    {
        private const int AdminWorkspaceID = -1;
        private const int WorkspaceArtifactTypeID = 8;

        private readonly IHelper _helper;
        private readonly ILog _logger;

        public SampleService(IHelper helper, ILog logger)
        {
            _helper = helper;
            _logger = logger.ForContext<SampleService>();
        }

        public async Task<WorkspaceSummary> GetWorkspaceSummaryAsync(int workspaceID)
        {
            if (workspaceID <= 0)
            {
                throw new ValidationException("workspaceID must be a positive Artifact ID.");
            }

            // ExecutionIdentity.CurrentUser runs the call as the caller, so Relativity's own
            // permissions decide what they can see. Use ExecutionIdentity.System only after the
            // service has confirmed the caller is allowed to do what it is about to do.
            using (IObjectManager objectManager =
                _helper.GetServicesManager().CreateProxy<IObjectManager>(ExecutionIdentity.CurrentUser))
            {
                var query = new QueryRequest
                {
                    ObjectType = new ObjectTypeRef { ArtifactTypeID = WorkspaceArtifactTypeID },
                    Condition = $"'ArtifactID' == {workspaceID}",
                    Fields = new[] { new FieldRef { Name = "Name" } },
                };

                QueryResult result = await objectManager
                    .QueryAsync(AdminWorkspaceID, query, 1, 1)
                    .ConfigureAwait(false);

                RelativityObject workspace = result.Objects.FirstOrDefault();
                if (workspace == null)
                {
                    // Log with message templates and named holes, never string interpolation.
                    _logger.LogWarning("Workspace {WorkspaceID} was not found or is not visible to the caller.", workspaceID);
                    throw new WorkspaceNotFoundException($"Workspace {workspaceID} was not found.");
                }

                return new WorkspaceSummary
                {
                    ArtifactID = workspace.ArtifactID,
                    Name = workspace.FieldValues.First().Value as string,
                };
            }
        }

        public Task<EchoResponse> EchoAsync(EchoRequest request)
        {
            if (request == null)
            {
                throw new ValidationException("The body must look like { \"request\": { \"Message\": \"...\" } }.");
            }

            return Task.FromResult(new EchoResponse
            {
                Message = request.Message,
                ReceivedUtc = DateTime.UtcNow,
            });
        }

        public void Dispose()
        {
        }
    }
}
