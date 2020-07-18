using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DotNetCoreApiCrmTestForPCA.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        // YOU WILL HAVE TO MANUALLY CREATE AN APPLICATION USER IN CRM INSTANCE FOLLOWING THE INSTRUCTIONS
        // https://docs.microsoft.com/en-us/previous-versions/dynamicscrm-2016/developers-guide/mt790170%28v%3dcrm.8%29

        //FOLLOW THE STEPS HERE TO REGISTER AN APPLICATION IN AZURE AD
        //AND CREATE AN APPLICATION USER IN CRM
        //https://msdn.microsoft.com/en-us/library/mt790171.aspx

        //This was registered in Azure AD as a WEB APPLICATION AND/OR WEB API

        //Azure Application / Client ID
        private const string ClientId = "346f5ffd-014c-44ab-9fbb-c4dacfa35663";
        //Azure Application Client Key / Secret
        private const string ClientSecret = "IU@l/ApIdo:3AduK4XXmZv6f=sHvitA4";

        //Resource / CRM Url
        private const string Resource = "https://pcasandbox.api.crm3.dynamics.com/";

        //Guid is your Azure Active Directory Tenant Id
        private const string Authority = "https://login.microsoftonline.com/38bd4854-32c7-416e-b921-5495008146cd/oauth2/token";

        private static string _accessToken;

        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IEnumerable<WeatherForecast> Get()
        {

            var getTokenTask = Task.Run(async () => await GetToken());
            Task.WaitAll(getTokenTask);

            if (getTokenTask.Result == null)
                return null;

            //Deserialize the token response to get the access token
            TokenResponse tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(getTokenTask.Result);
            _accessToken = tokenResponse.access_token;

            Task.WaitAll(Task.Run(async () => await GetAccounts()));

            return null;
        }

        private async Task<string> GetToken()
        {
            using (HttpClient httpClient = new HttpClient())
            {
                var formContent = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("resource", Resource),
                    new KeyValuePair<string, string>("client_id", ClientId),
                    new KeyValuePair<string, string>("client_secret", ClientSecret),
                    new KeyValuePair<string, string>("grant_type", "client_credentials")
                });

                HttpResponseMessage response = await httpClient.PostAsync(Authority, formContent);

                return !response.IsSuccessStatusCode ? null
                    : response.Content.ReadAsStringAsync().Result;
            }
        }


        private async Task GetAccounts()
        {
            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri(Resource);
                httpClient.Timeout = new TimeSpan(0, 2, 0);
                httpClient.DefaultRequestHeaders.Add("OData-MaxVersion", "4.0");
                httpClient.DefaultRequestHeaders.Add("OData-Version", "4.0");
                httpClient.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));
                httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", _accessToken);

                //Retrieve 
                HttpResponseMessage retrieveResponse =
                    await httpClient.GetAsync("api/data/v9.1/accounts?$select=name&$top=3");
                if (retrieveResponse.IsSuccessStatusCode)
                {
                    JObject jRetrieveResponse =
                        JObject.Parse(retrieveResponse.Content.ReadAsStringAsync().Result);
                    string fullname = jRetrieveResponse["fullname"].ToString();

                    _logger.LogInformation("Fullname " + fullname);

                }
                else
                    return;
            }
        }

        private async Task DoWork()
        {
            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri(Resource);
                httpClient.Timeout = new TimeSpan(0, 2, 0);
                httpClient.DefaultRequestHeaders.Add("OData-MaxVersion", "4.0");
                httpClient.DefaultRequestHeaders.Add("OData-Version", "4.0");
                httpClient.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));
                httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", _accessToken);

                //Retrieve 
                HttpResponseMessage retrieveResponse =
                    await httpClient.GetAsync("api/data/v9.1/accounts?$select=name&$top=3");
                if (retrieveResponse.IsSuccessStatusCode)
                {
                    JObject jRetrieveResponse =
                        JObject.Parse(retrieveResponse.Content.ReadAsStringAsync().Result);
                    string fullname = jRetrieveResponse["fullname"].ToString();

                    _logger.LogInformation("Fullname " + fullname);

                }
                else
                    return;

                //Unbound Function
                HttpResponseMessage whoAmIResponse =
                    await httpClient.GetAsync("api/data/v9.1/WhoAmI");
                Guid userId;
                if (whoAmIResponse.IsSuccessStatusCode)
                {
                    JObject jWhoAmIResponse =
                        JObject.Parse(whoAmIResponse.Content.ReadAsStringAsync().Result);
                    userId = (Guid)jWhoAmIResponse["UserId"];
                    _logger.LogInformation("WhoAmI " + userId);
                }
                else
                    return;

                

                ////Create
                //JObject newAccount = new JObject
                //{
                //    {"name", "CSharp Test"},
                //    {"telephone1", "111-888-7777"}
                //};

                //HttpResponseMessage createResponse =
                //    await httpClient.SendAsJsonAsync(HttpMethod.Post, "api/data/v8.2/accounts", newAccount);

                //Guid accountId = new Guid();
                //if (createResponse.IsSuccessStatusCode)
                //{
                //    string accountUri = createResponse.Headers.GetValues("OData-EntityId").FirstOrDefault();
                //    if (accountUri != null)
                //        accountId = Guid.Parse(accountUri.Split('(', ')')[1]);

                //    Console.WriteLine("Account '{0}' created.", newAccount["name"]);
                //}
                //else
                //    return;

                ////Update 
                //newAccount.Add("fax", "123-456-7890");

                //HttpResponseMessage updateResponse =
                //    await httpClient.SendAsJsonAsync(new HttpMethod("PATCH"), "api/data/v8.2/accounts(" + accountId + ")", newAccount);
                //if (updateResponse.IsSuccessStatusCode)
                //    Console.WriteLine("Account '{0}' updated", newAccount["name"]);

                ////Delete
                //HttpResponseMessage deleteResponse =
                //    await httpClient.DeleteAsync("api/data/v8.2/accounts(" + accountId + ")");

                //if (deleteResponse.IsSuccessStatusCode)
                //    Console.WriteLine("Account '{0}' deleted", newAccount["name"]);
            }
        }
    }
}
