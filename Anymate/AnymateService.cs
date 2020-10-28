using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Anymate.Helpers;
using Anymate.Models;
using Newtonsoft.Json;

namespace Anymate
{
    public class AnymateService : IAnymateService
    {
        private static string AnymateUrl(string customerKey) => $"https://{customerKey}.anymate.app";
        private static string AnymateAuthUrl(string customerKey) => $"https://{customerKey}.auth.anymate.app";
        private static string OnPremisesApiUrl { get; set; }
        private static string OnPremisesAuthUrl { get; set; }
        private bool OnPremisesMode { get; set; } = false;
        private AuthTokenRequest _request { get; set; } = new AuthTokenRequest();

        public string AccessToken
        {
            get => _request.access_token;
            set => _request.access_token = value;
        }

        public bool HasAuthCredentials => AuthTokenRequestIsValid();

        private bool AuthTokenRequestIsValid()
        {
            if (_request == null)
                return false;

            if (string.IsNullOrWhiteSpace(_request.client_id))
                return false;

            if (string.IsNullOrWhiteSpace(_request.client_secret))
                return false;

            if (string.IsNullOrWhiteSpace(_request.password))
                return false;

            if (string.IsNullOrWhiteSpace(_request.username))
                return false;


            return true;
        }

        /// <summary>
        /// This is the default way of using the AnymateClient. It assumes you are using the cloud version of Anymate.
        /// </summary>
        /// <remarks>
        /// No need for any further configuration, as the Anymate Client automatically will setup itself once you log in.
        /// </remarks>
        public AnymateService(string customerKey, string secret, string username, string password)
        {
            OnPremisesMode = false;
            _request = new AuthTokenRequest()
            {
                client_id = customerKey,
                client_secret = secret,
                password = password,
                username = username
            };
        }


        /// <summary>
        /// In order to run in OnPremisesMode, supply an Client Uri and Auth Uri
        /// </summary>
        /// <remarks>
        /// Supplying an Client Uri and Auth Uri will trigger the client to work in OnPremises mode, using the supplied url's instead of the cloud version of Anymate.
        /// </remarks>
        /// <param name="clientUri">The url for the anymate client installation. Url should just be the domain with http/https in front, similar to "https://customer.anymate.app"</param>
        /// <param name="authUri">The url for the anymate auth server installation. Url should just be the domain with http/https in front, similar to "https://customer.auth.anymate.app"</param>
        public AnymateService(string customerKey, string secret, string username, string password, string clientUri,
            string authUri)
        {
            OnPremisesMode = true;
            _request = new AuthTokenRequest()
            {
                client_id = customerKey,
                client_secret = secret,
                password = password,
                username = username
            };
            clientUri = clientUri.Trim();
            if (clientUri.EndsWith("/"))
                clientUri = clientUri.Substring(0, clientUri.Length - 1);

            authUri = authUri.Trim();
            if (authUri.EndsWith("/"))
                authUri = authUri.Substring(0, authUri.Length - 1);

            OnPremisesApiUrl = clientUri;
            OnPremisesAuthUrl = authUri;
        }

        private string GetAnymateUrl(string customerKey)
        {
            if (OnPremisesMode)
            {
                return OnPremisesApiUrl;
            }
            else
            {
                return AnymateUrl(customerKey);
            }
        }

        private string GetAuthUrl(string customerKey)
        {
            if (OnPremisesMode)
            {
                return OnPremisesAuthUrl;
            }
            else
            {
                return AnymateAuthUrl(customerKey);
            }
        }


        private Dictionary<string, string> GetFormDataPasswordAuth(AuthTokenRequest request)
        {
            var values = CreateFormData(request);
            values.Add("grant_type", "password");
            values.Add(nameof(request.username), request.username);
            values.Add(nameof(request.password), request.password);
            return values;
        }

        private Dictionary<string, string> CreateFormData(AuthTokenRequest request)
        {
            var values = new Dictionary<string, string>();
            values.Add(nameof(request.client_id), request.client_id);
            values.Add(nameof(request.client_secret), request.client_secret);

            return values;
        }

        private async Task GetOrRefreshAccessTokenAsync()
        {
            var result = await GetOrRefreshAccessTokenAsync(_request);
            if (!result.Succeeded)
                throw new Exception($"Could not authenticate. Got message: {result.HttpMessage}");
        }

        private async Task<AuthResponse> GetOrRefreshAccessTokenAsync(AuthTokenRequest request)
        {

            if (string.IsNullOrWhiteSpace(request.access_token) || !TokenValidator.RefreshNotNeeded(request.access_token))
            {
                if (string.IsNullOrWhiteSpace(request.client_id))
                    throw new ArgumentNullException("client_id was null.");
                if (string.IsNullOrWhiteSpace(request.client_secret))
                    throw new ArgumentNullException("client_secret was null.");


                if (string.IsNullOrWhiteSpace(request.password) || string.IsNullOrWhiteSpace(request.username))
                {
                    throw new ArgumentNullException(
                        "Found no refresh token and either username or password were empty.");
                }

                var token = await GetAuthTokenPasswordFlowAsync(request);
                return token;
            }
            else
            {
                return new AuthResponse()
                {
                    access_token = _request.access_token,
                    Succeeded = true
                };
            }
        }

        private async Task<AuthResponse> GetAuthTokenPasswordFlowAsync(AuthTokenRequest request)
        {
            var formData = GetFormDataPasswordAuth(request);
            return await GetAuthToken(formData, request.client_id);
        }


        private async Task<AuthResponse> GetAuthToken(Dictionary<string, string> formData, string customerKey)
        {
            var values = formData;

            var content = new FormUrlEncodedContent(values);
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.Timeout = TimeSpan.FromMinutes(5);
                using (HttpResponseMessage response = await client.PostAsync($"{GetAuthUrl(customerKey)}/connect/token", content))
                {
                    using (HttpContent responseContent = response.Content)
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            string data = await responseContent.ReadAsStringAsync();
                            var json = JsonConvert.DeserializeObject<AuthResponse>(data);
                            json.HttpMessage = $"{response.StatusCode} - {response.ReasonPhrase}";
                            _request.access_token = json.access_token;
                            return json;
                        }
                        else
                        {
                            var result = AuthResponse.Failed;
                            result.HttpMessage = $"{response.StatusCode} - {response.ReasonPhrase}";
                            _request.access_token = string.Empty;
                            return result;
                        }
                    }
                }
            }
        }


        private string GetCustomerKeyFromToken(string access_token)
        {
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadToken(access_token) as JwtSecurityToken;
            var customerKey = jsonToken.Claims.FirstOrDefault(claim => claim.Type == "auth.anymate.app/CustomerKey")
                .Value;

            if (string.IsNullOrWhiteSpace(customerKey))
                throw new Exception("Token invalid");

            return customerKey;
        }

        private async Task<string> CallApiPostAsync(string endpoint, string jsonPayload)
        {
            await GetOrRefreshAccessTokenAsync();
            var customerKey = GetCustomerKeyFromToken(_request.access_token);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _request.access_token);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.Timeout = TimeSpan.FromMinutes(5);
                using (HttpResponseMessage response = await client.PostAsync(GetAnymateUrl(customerKey) + endpoint, content))
                using (HttpContent responseContent = response.Content)
                {
                    string data = await responseContent.ReadAsStringAsync();
                    return data;
                }
            }
        }


        private async Task<string> CallApiGetAsync(string endpoint)
        {
            await GetOrRefreshAccessTokenAsync();
            var customerKey = GetCustomerKeyFromToken(_request.access_token);
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _request.access_token);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.Timeout = TimeSpan.FromMinutes(5);
                using (HttpResponseMessage response = await client.GetAsync(GetAnymateUrl(customerKey) + endpoint))
                using (HttpContent responseContent = response.Content)
                {
                    string data = await responseContent.ReadAsStringAsync();
                    return data;
                }
            }
        }


        public async Task<AnymateResponse> FailureAsync(string payload)
        {
            return await FailureAsync<AnymateResponse>(payload);
        }

        public async Task<TResponse> FailureAsync<TResponse>(string payload)
        {
            var endpoint = $"/apimate/Failure/";
            var response = await CallApiPostAsync(endpoint, payload);
            return JsonConvert.DeserializeObject<TResponse>(response);
        }

        public async Task<AnymateResponse> FailureAsync(AnymateProcessFailure action)
        {
            return await FailureAsync<AnymateProcessFailure>(action);
        }

        public async Task<AnymateResponse> FailureAsync<T>(T action)
        {
            return await FailureAsync<AnymateResponse, T>(action);
        }

        public async Task<TResponse> FailureAsync<TResponse, TAction>(TAction action)
        {
            var payload = JsonConvert.SerializeObject(action);
            return await FailureAsync<TResponse>(payload);
        }


        public async Task<AnymateResponse> FinishRunAsync(string payload)
        {
            return await FinishRunAsync<AnymateResponse>(payload);
        }

        public async Task<TResponse> FinishRunAsync<TResponse>(string payload)
        {
            var endpoint = $"/apimate/FinishRun/";
            var response = await CallApiPostAsync(endpoint, payload);
            return JsonConvert.DeserializeObject<TResponse>(response);
        }

        public async Task<AnymateResponse> FinishRunAsync(AnymateFinishRun action)
        {
            return await FinishRunAsync<AnymateFinishRun>(action);
        }

        public async Task<AnymateResponse> FinishRunAsync<T>(T action)
        {
            return await FinishRunAsync<AnymateResponse, T>(action);
        }

        public async Task<TResponse> FinishRunAsync<TResponse, TAction>(TAction action)
        {
            var payload = JsonConvert.SerializeObject(action);
            return await FinishRunAsync<TResponse>(payload);
        }

        public async Task<AnymateRunResponse> StartOrGetRunAsync(string processKey)
        {
            return await StartOrGetRunAsync<AnymateRunResponse>(processKey);
        }

        public async Task<T> StartOrGetRunAsync<T>(string processKey)
        {
            var endpoint = $"/apimate/StartOrGetRun/{processKey}";
            var response = await CallApiGetAsync(endpoint);
            return JsonConvert.DeserializeObject<T>(response);
        }

        public async Task<T> OkToRunAsync<T>(string processKey)
        {
            var endpoint = $"/apimate/OkToRun/{processKey}";
            var response = await CallApiGetAsync(endpoint);
            return JsonConvert.DeserializeObject<T>(response);
        }

        public async Task<AnymateOkToRun> OkToRunAsync(string processKey)
        {
            return await OkToRunAsync<AnymateOkToRun>(processKey);
        }


        public async Task<T> GetRulesAsync<T>(string processKey)
        {
            var jsonResult = await GetRulesAsync(processKey);
            var result = JsonConvert.DeserializeObject<T>(jsonResult);
            return result;
        }

        public async Task<string> GetRulesAsync(string processKey)
        {
            var endpoint = $"/apimate/GetRules/{processKey}";
            return await CallApiGetAsync(endpoint);
        }

        public async Task<string> TakeNextAsync(string processKey)
        {
            var endpoint = $"/apimate/TakeNext/{processKey}";
            return await CallApiGetAsync(endpoint);
        }

        public async Task<T> TakeNextAsync<T>(string processKey)
        {
            var jsonResult = await TakeNextAsync(processKey);
            var result = JsonConvert.DeserializeObject<T>(jsonResult);
            return result;
        }

        public async Task<AnymateResponse> CreateTaskAsync<T>(T newTask, string processKey)
        {
            return await CreateTaskAsync<AnymateResponse, T>(newTask, processKey);
        }

        public async Task<TResponse> CreateTaskAsync<TResponse, TModel>(TModel newTask, string processKey)
        {
            var payload = JsonConvert.SerializeObject(newTask);
            return await CreateTaskAsync<TResponse>(payload, processKey);
        }

        public async Task<TResponse> CreateTaskAsync<TResponse>(string payload, string processKey)
        {
            var endpoint = $"/apimate/CreateTask/{processKey}";
            var response = await CallApiPostAsync(endpoint, payload);
            return JsonConvert.DeserializeObject<TResponse>(response);
        }

        public async Task<AnymateResponse> UpdateTaskAsync<T>(T updateTask)
        {
            return await UpdateTaskAsync<AnymateResponse, T>(updateTask);
        }

        public async Task<TResponse> UpdateTaskAsync<TResponse>(string payload)
        {
            var endpoint = $"/apimate/UpdateTask/";

            var response = await CallApiPostAsync(endpoint, payload);
            return JsonConvert.DeserializeObject<TResponse>(response);
        }

        public async Task<TResponse> UpdateTaskAsync<TResponse, TUpdate>(TUpdate updateTask)
        {
            var payload = JsonConvert.SerializeObject(updateTask);
            return await UpdateTaskAsync<TResponse>(payload);
        }

        public async Task<string> CreateAndTakeTaskAsync<T>(T newTask, string processKey)
        {
            var payload = JsonConvert.SerializeObject(newTask);
            return await CreateAndTakeTaskAsync(payload, processKey);
        }

        public async Task<string> CreateAndTakeTaskAsync(string payload, string processKey)
        {
            var endpoint = $"/apimate/CreateAndTakeTask/{processKey}";
            var response = await CallApiPostAsync(endpoint, payload);
            return response;
        }

        public async Task<T> ErrorAsync<T>(string payload)
        {
            var endpoint = $"/apimate/Error/";
            var response = await CallApiPostAsync(endpoint, payload);
            return JsonConvert.DeserializeObject<T>(response);
        }

        public async Task<AnymateResponse> ErrorAsync(string payload)
        {
            return await ErrorAsync<AnymateResponse>(payload);
        }

        public async Task<AnymateResponse> ErrorAsync(AnymateTaskAction action)
        {
            return await ErrorAsync<AnymateTaskAction>(action);
        }

        public async Task<AnymateResponse> ErrorAsync<T>(T action)
        {
            return await ErrorAsync<AnymateResponse, T>(action);
        }

        public async Task<TResponse> ErrorAsync<TResponse, TAction>(TAction action)
        {
            var payload = JsonConvert.SerializeObject(action);
            return await ErrorAsync<TResponse>(payload);
        }


        public async Task<AnymateResponse> RetryAsync(AnymateTaskAction action)
        {
            return await RetryAsync<AnymateTaskAction>(action);
        }

        public async Task<AnymateResponse> RetryAsync(string payload)
        {
            return await RetryAsync<AnymateResponse>(payload);
        }

        public async Task<TResponse> RetryAsync<TResponse>(string payload)
        {
            var endpoint = $"/apimate/Retry/";
            var response = await CallApiPostAsync(endpoint, payload);
            return JsonConvert.DeserializeObject<TResponse>(response);
        }

        public async Task<TResponse> RetryAsync<TResponse, TAction>(TAction action)
        {
            var payload = JsonConvert.SerializeObject(action);
            return await RetryAsync<TResponse>(payload);
        }

        public async Task<AnymateResponse> RetryAsync<T>(T action)
        {
            return await RetryAsync<AnymateResponse, T>(action);
        }

        public async Task<AnymateResponse> ManualAsync(string payload)
        {
            return await ManualAsync<AnymateResponse>(payload);
        }

        public async Task<TResponse> ManualAsync<TResponse>(string payload)
        {
            var endpoint = $"/apimate/Manual/";
            var response = await CallApiPostAsync(endpoint, payload);
            return JsonConvert.DeserializeObject<TResponse>(response);
        }

        public async Task<AnymateResponse> ManualAsync(AnymateTaskAction action)
        {
            return await ManualAsync<AnymateTaskAction>(action);
        }

        public async Task<AnymateResponse> ManualAsync<T>(T action)
        {
            return await ManualAsync<AnymateResponse, T>(action);
        }

        public async Task<TResponse> ManualAsync<TResponse, TAction>(TAction action)
        {
            var payload = JsonConvert.SerializeObject(action);
            return await ManualAsync<TResponse>(payload);
        }

        public async Task<AnymateResponse> SolvedAsync(string payload)
        {
            return await SolvedAsync<AnymateResponse>(payload);
        }

        public async Task<TResponse> SolvedAsync<TResponse>(string payload)
        {
            var endpoint = $"/apimate/Solved/";
            var response = await CallApiPostAsync(endpoint, payload);
            return JsonConvert.DeserializeObject<TResponse>(response);
        }

        public async Task<AnymateResponse> SolvedAsync(AnymateTaskAction action)
        {
            return await SolvedAsync<AnymateTaskAction>(action);
        }

        public async Task<AnymateResponse> SolvedAsync<T>(T action)
        {
            return await SolvedAsync<AnymateResponse, T>(action);
        }

        public async Task<TResponse> SolvedAsync<TResponse, TAction>(TAction action)
        {
            var payload = JsonConvert.SerializeObject(action);
            return await SolvedAsync<TResponse>(payload);
        }
    }
}