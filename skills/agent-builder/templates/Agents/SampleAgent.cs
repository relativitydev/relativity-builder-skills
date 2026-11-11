using System;
using System.Collections.Generic;
using System.Linq;
using kCura.Agent;
using kCura.Agent.CustomAttributes;
using Relativity.API;
using Relativity.Services.Objects;
using Relativity.Services.Objects.DataContracts;

namespace MyCompany.MyApp.Agents
{
    // [Name] is the agent type name shown in the Agents tab.
    // The GUID identifies this agent type. Generate a new one for every agent you write and never
    // change it after the agent has been deployed. Always fully qualify the attribute: with
    // "using System;" in scope, a bare [Guid(...)] binds to the System.Guid struct and fails with CS0616.
    //
    // Optional Workload Discovery (see templates/WorkloadDiscovery): uncomment the [Path] line, add
    // the Relativity.Platform.Agent.WorkloadDiscovery.SDK package, and put this agent's own GUID in
    // the path. No leading slash. This lets the run interval go as low as 60 seconds.
    [Name("Sample Agent")]
    [System.Runtime.InteropServices.Guid("00000000-0000-0000-0000-000000000000")]
    //[WorkloadDiscovery.CustomAttributes.Path("relativity.rest/api/my-agents/v1/workload-discovery/00000000-0000-0000-0000-000000000000/workload")]
    public class SampleAgent : AgentBase
    {
        // Process a bounded batch per run. The agent runs again on its next interval, so it is
        // better to do a small, safe amount of work each time than to try to drain everything.
        private const int BatchSize = 100;

        public override string Name => "Sample Agent";

        public override void Execute()
        {
            IAPILog logger = Helper.GetLoggerFactory().GetLogger().ForContext<SampleAgent>();

            try
            {
                RaiseMessage("Checking for work.", 10);

                IReadOnlyList<int> workItems = GetPendingWork();
                if (workItems.Count == 0)
                {
                    RaiseMessage("No work found.", 10);
                    return;
                }

                int processed = 0;
                foreach (int workItem in workItems)
                {
                    // Relativity sets ToBeRemoved when it wants this agent instance to stop
                    // (for example, during scale-down or a deployment). Stop at a safe point.
                    if (ToBeRemoved)
                    {
                        logger.LogInformation("Stop requested after {Processed} of {Total} items.", processed, workItems.Count);
                        break;
                    }

                    ProcessWorkItem(workItem);
                    processed++;
                }

                RaiseMessage($"Processed {processed} item(s).", 10);
                logger.LogInformation("Processed {Processed} work item(s).", processed);
            }
            catch (Exception ex)
            {
                // Use message templates with named holes, never string interpolation, in log calls.
                logger.LogError(ex, "{AgentName} failed.", Name);
                RaiseError($"{Name} failed: {ex.Message}", ex.ToString());
            }
        }

        private IReadOnlyList<int> GetPendingWork()
        {
            // Replace with your own source of work: a queue table, an Object Manager query for
            // objects in a "Pending" status, and so on. Keep it bounded by BatchSize.
            return Enumerable.Empty<int>().Take(BatchSize).ToList();
        }

        private void ProcessWorkItem(int workItem)
        {
            // Example of calling a Relativity API from an agent. Agents have no signed-in user,
            // so proxies use ExecutionIdentity.System.
            // Execute() is synchronous, so block on async calls with GetAwaiter().GetResult().
            using (IObjectManager objectManager =
                Helper.GetServicesManager().CreateProxy<IObjectManager>(ExecutionIdentity.System))
            {
                // var result = objectManager.ReadAsync(workspaceId, new ReadRequest { ... })
                //     .GetAwaiter().GetResult();
            }
        }
    }
}
