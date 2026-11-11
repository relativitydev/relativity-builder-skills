using System;
using System.Threading.Tasks;
using Relativity.Kepler.Services;
using WorkloadDiscovery;

namespace MyCompany.MyApp.Interfaces.MyAgents.v1
{
    // Relativity polls this endpoint at the agent's run interval to decide how many agent
    // processes to run. Resulting URL (matches the agent's [Path] attribute):
    //   relativity.rest/api/my-agents/v1/workload-discovery/{agentGuid}/workload
    // It must answer within 2 seconds and return WorkloadSize.None when there is no work.
    // Name the interface after the agent (ISampleAgentWorkloadService), never a generic name such
    // as IWorkloadDiscoveryService. Kepler refuses to host two services with the same interface
    // name, and that error can stop EVERY Kepler service in the application (all return 404).
    [WebService("Sample Agent Workload Discovery Service")]
    [ServiceAudience(Audience.Private)]
    [RoutePrefix("workload-discovery")]
    public interface ISampleAgentWorkloadService : IDisposable
    {
        [HttpGet]
        [Route("{agentGuid:guid}/workload")]
        Task<Workload> GetWorkloadAsync(Guid agentGuid);
    }
}
