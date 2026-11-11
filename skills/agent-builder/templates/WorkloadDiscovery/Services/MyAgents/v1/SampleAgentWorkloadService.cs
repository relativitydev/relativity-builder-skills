using System;
using System.Threading.Tasks;
using MyCompany.MyApp.Interfaces.MyAgents.v1;
using Relativity.API;
using Relativity.Kepler.Logging;
using WorkloadDiscovery;

namespace MyCompany.MyApp.Services.MyAgents.v1
{
    public class SampleAgentWorkloadService : ISampleAgentWorkloadService
    {
        // Must match the [System.Runtime.InteropServices.Guid] on SampleAgent.
        private static readonly Guid SampleAgentGuid = new Guid("00000000-0000-0000-0000-000000000000");

        private readonly IHelper _helper;
        private readonly ILog _logger;

        public SampleAgentWorkloadService(IHelper helper, ILog logger)
        {
            _helper = helper;
            _logger = logger.ForContext<SampleAgentWorkloadService>();
        }

        public async Task<Workload> GetWorkloadAsync(Guid agentGuid)
        {
            if (agentGuid != SampleAgentGuid)
            {
                return new Workload { Size = WorkloadSize.None };
            }

            try
            {
                int pending = await CountPendingWorkAsync().ConfigureAwait(false);
                return new Workload { Size = ToSize(pending) };
            }
            catch (Exception ex)
            {
                // Fail toward running one process rather than none, so work is not stranded.
                _logger.LogError(ex, "Workload discovery failed for agent {AgentGuid}.", agentGuid);
                return new Workload { Size = WorkloadSize.One };
            }
        }

        private Task<int> CountPendingWorkAsync()
        {
            // Replace with a FAST count of your queue (it must answer within 2 seconds),
            // for example an Object Manager query with length 1 that reads TotalCount.
            return Task.FromResult(0);
        }

        private static WorkloadSize ToSize(int pending)
        {
            // Tune the thresholds to how much work one agent process handles per interval.
            // Lightweight or steady workloads should not scale beyond One.
            if (pending <= 0) return WorkloadSize.None;
            if (pending < 100) return WorkloadSize.One;
            if (pending < 1000) return WorkloadSize.S;
            if (pending < 5000) return WorkloadSize.M;
            return WorkloadSize.L;
        }

        public void Dispose()
        {
        }
    }
}
