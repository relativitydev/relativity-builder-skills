using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MyCompany.MyApp.Interfaces.MyModule.v1;
using MyCompany.MyApp.Interfaces.MyModule.v1.Models;
using Relativity.Services.ServiceProxy;

namespace MyCompany.MyApp.TestClient
{
    // Usage (set environment variables first; never hard-code secrets):
    //   RELATIVITY_INSTANCE_URL  e.g. https://yourtenant.relativity.one
    //   RELATIVITY_CLIENT_ID     OAuth2 client (client credentials flow) from the OAuth2 Client tab
    //   RELATIVITY_CLIENT_SECRET
    //   RELATIVITY_WORKSPACE_ID  a workspace the OAuth2 client's user can see
    internal static class Program
    {
        private static async Task<int> Main()
        {
            string instanceUrl = Require("RELATIVITY_INSTANCE_URL").TrimEnd('/');
            string clientId = Require("RELATIVITY_CLIENT_ID");
            string clientSecret = Require("RELATIVITY_CLIENT_SECRET");
            int workspaceId = int.Parse(Require("RELATIVITY_WORKSPACE_ID"));

            string token = await GetBearerTokenAsync(instanceUrl, clientId, clientSecret);

            var settings = new ServiceFactorySettings(
                new Uri(instanceUrl + "/Relativity.REST/api"),
                new BearerTokenCredentials(token));
            var factory = new ServiceFactory(settings);

            int failures = 0;
            using (ISampleService service = factory.CreateProxy<ISampleService>())
            {
                failures += await RunAsync("GetWorkspaceSummaryAsync", async () =>
                {
                    WorkspaceSummary summary = await service.GetWorkspaceSummaryAsync(workspaceId);
                    return $"ArtifactID={summary.ArtifactID}, Name={summary.Name}";
                });

                failures += await RunAsync("EchoAsync", async () =>
                {
                    EchoResponse echo = await service.EchoAsync(new EchoRequest { Message = "hello" });
                    return $"Message={echo.Message}, ReceivedUtc={echo.ReceivedUtc:u}";
                });
            }

            return failures == 0 ? 0 : 1;
        }

        private static async Task<int> RunAsync(string name, Func<Task<string>> call)
        {
            Console.WriteLine($"Calling {name} ...");
            try
            {
                Console.WriteLine("  OK: " + await call());
                return 0;
            }
            catch (Exception ex)
            {
                // ServiceNotFoundException here means the server has not hosted the service:
                // DLLs not uploaded, not linked to the application, or a required attribute is missing.
                Console.WriteLine($"  FAILED: {ex.GetType().Name}: {ex.Message}");
                return 1;
            }
        }

        private static async Task<string> GetBearerTokenAsync(string instanceUrl, string clientId, string clientSecret)
        {
            using (var http = new HttpClient())
            {
                var body = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "client_credentials",
                    ["client_id"] = clientId,
                    ["client_secret"] = clientSecret,
                    ["scope"] = "UserInfoAccess",
                });

                HttpResponseMessage response = await http.PostAsync(instanceUrl + "/Relativity/Identity/connect/token", body);
                string json = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException($"Token request failed ({(int)response.StatusCode}): {json}");
                }

                Match match = Regex.Match(json, "\"access_token\"\\s*:\\s*\"([^\"]+)\"");
                if (!match.Success)
                {
                    throw new InvalidOperationException("Token response did not contain an access_token.");
                }

                return match.Groups[1].Value;
            }
        }

        private static string Require(string name)
        {
            string value = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException($"Set the {name} environment variable.");
            }

            return value;
        }
    }
}
