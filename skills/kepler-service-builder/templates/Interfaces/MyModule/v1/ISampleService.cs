using System;
using System.Threading.Tasks;
using MyCompany.MyApp.Interfaces.MyModule.v1.Models;
using Relativity.Kepler.Services;

namespace MyCompany.MyApp.Interfaces.MyModule.v1
{
    // The ".v1" namespace segment produces the /v1/ URL segment (VersioningStrategy.Namespace).
    // Required on every service interface:
    //   [WebService] (non-empty), [ServiceAudience] (deployment fails without it),
    //   [RoutePrefix] (non-empty), inherit IDisposable, every method returns Task or Task<T>.
    //
    // REST base: {host}/Relativity.REST/api/my-module/v1/sample-service/
    [WebService("Sample Service")]
    [ServiceAudience(Audience.Public)]
    [RoutePrefix("sample-service")]
    public interface ISampleService : IDisposable
    {
        // GET .../sample-service/workspace/1234567
        // Every parameter of a GET must appear in the route; anything else is read from the body.
        [HttpGet]
        [Route("workspace/{workspaceID:int}")]
        Task<WorkspaceSummary> GetWorkspaceSummaryAsync(int workspaceID);

        // POST .../sample-service/echo
        // Kepler wraps POST bodies by parameter name, so the JSON body is:
        //   { "request": { "Message": "hello" } }
        [HttpPost]
        [Route("echo")]
        Task<EchoResponse> EchoAsync(EchoRequest request);
    }
}
