using Relativity.Kepler.Services;

namespace MyCompany.MyApp.Interfaces.MyAgents
{
    // Route prefix must not be empty and must not contain the word "Relativity".
    [ServiceModule("My Agents Module")]
    [RoutePrefix("my-agents", VersioningStrategy.Namespace)]
    public interface IMyAgentsModule
    {
    }
}
