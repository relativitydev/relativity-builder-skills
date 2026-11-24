using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Relativity.ObjectManager.V1.Interfaces;
using Relativity.ObjectManager.V1.Models;
using Relativity.Services.ServiceProxy;

namespace Contoso.Reports.Console
{
    // An aiR-ready client: OAuth2 bearer token, Object Manager SDK from nuget.org,
    // the X-Kepler-Referrer header, local temp files only.
    public static class Program
    {
        public static async Task Main()
        {
            string token = await GetTokenAsync();
            var settings = new ServiceFactorySettings(
                new Uri("https://contoso.relativity.one/Relativity.REST/api"),
                new BearerTokenCredentials(token));
            settings.Headers["X-Kepler-Referrer"] = "contoso-reports";
            var factory = new ServiceFactory(settings);

            using (IObjectManager om = factory.CreateProxy<IObjectManager>())
            {
                var query = new QueryRequest
                {
                    ObjectType = new ObjectTypeRef { ArtifactTypeID = 10 },
                    Condition = "'Reviewed' == false",
                    Fields = new[] { new FieldRef { Name = "Control Number" } }
                };
                QueryResult result = await om.QueryAsync(1017123, query, 0, 100);
                string report = Path.Combine(Path.GetTempPath(), "unreviewed.csv");
                File.WriteAllText(report, result.TotalCount.ToString());
            }
        }

        private static async Task<string> GetTokenAsync()
        {
            using (var http = new HttpClient())
            {
                var response = await http.PostAsync(
                    "https://contoso.relativity.one/Relativity/Identity/connect/token",
                    new FormUrlEncodedContent(new[]
                    {
                        new System.Collections.Generic.KeyValuePair<string, string>("grant_type", "client_credentials"),
                        new System.Collections.Generic.KeyValuePair<string, string>("scope", "SystemUserInfo"),
                    }));
                return await response.Content.ReadAsStringAsync();
            }
        }
    }
}
