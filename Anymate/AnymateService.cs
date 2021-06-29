using System;
using System.Collections.Generic;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Newtonsoft.Json;

namespace Anymate
{
    public class AnymateService : IAnymateService
    {
        // HttpClient has some inherent problems if you follow the normal using pattern, used for IDisposable classes. 
        // These are described here:
        // https://docs.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests
        // In order to combat these problems, we have gone with a single HttpClient in AnymateService.
        // We have followed the guidelines described here: https://docs.microsoft.com/en-us/dotnet/csharp/tutorials/console-webapiclient
        // If this does not solve the problems of HttpClient, we will switch to RestSharp.
        // Google keywords are : HttpClient, HttpClientFactory, Socket Exhaustion
        // Exception was: an existing connection was forcibly closed by the remote host

        private HttpClient _httpClient;

        private static string AnymateUrl(string customerKey) => $"https://{customerKey}.anymate.app";
        private static string AnymateAuthUrl(string customerKey) => $"https://{customerKey}.auth.anymate.app";
        private static string OnPremisesApiUrl { get; set; }
        private static string OnPremisesAuthUrl { get; set; }
        private bool OnPremisesMode { get; set; } = false;
        private AuthTokenRequest _request { get; set; }

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
            _httpClient =  new HttpClient();
            _httpClient.Timeout = TimeSpan.FromMinutes(5);

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
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromMinutes(5);
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
            if (string.IsNullOrWhiteSpace(request.access_token) ||
                !TokenValidator.RefreshNotNeeded(request.access_token))
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
            
            var request = new HttpRequestMessage(HttpMethod.Post, $"{GetAuthUrl(customerKey)}/connect/token");
            request.Headers.Accept.Clear();
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = content;
           

            using (HttpResponseMessage response = await _httpClient.SendAsync(request, CancellationToken.None))
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

            var request = new HttpRequestMessage(HttpMethod.Post, GetAnymateUrl(customerKey) + endpoint);
            request.Headers.Accept.Clear();
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _request.access_token);
            request.Content = content;

           
            using (HttpResponseMessage response = await _httpClient.SendAsync(request, CancellationToken.None))
            {
                var responseMessage = response.EnsureSuccessStatusCode();
                using (HttpContent responseContent = response.Content)
                {
                    string data = await responseContent.ReadAsStringAsync();
                    return data;
                }
            }

        }

        private string CallApiPost(string endpoint, string jsonPayload)
        {
            return AsyncUtil.RunSync(() => CallApiPostAsync(endpoint, jsonPayload));
        }

        private async Task<string> CallApiGetAsync(string endpoint)
        {
            await GetOrRefreshAccessTokenAsync();
            var customerKey = GetCustomerKeyFromToken(_request.access_token);

            var request = new HttpRequestMessage(HttpMethod.Get, GetAnymateUrl(customerKey) + endpoint);
            request.Headers.Accept.Clear();
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _request.access_token);

            using (HttpResponseMessage response = await _httpClient.SendAsync(request, CancellationToken.None))
            {
                var responseMessage = response.EnsureSuccessStatusCode();
                using (HttpContent responseContent = response.Content)
                {
                    string data = await responseContent.ReadAsStringAsync();
                    return data;
                }

            }
        }

        private string CallApiGet(string endpoint)
        {
            return AsyncUtil.RunSync(() => CallApiGetAsync(endpoint));
        }


        #region Async Methods

        public async Task<AnymateResponse> FailureAsync(string payload)
        {
            return await FailureAsync<AnymateResponse>(payload);
        }

        public async Task<TResponse> FailureAsync<TResponse>(string payload)
        {
            var endpoint = $"/api/Failure/";
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

        public async Task<AnymateResponse> FailureAsync(string processKey, string message)
        {
            return await FailureAsync(new AnymateProcessFailure()
            { ProcessKey = processKey, Message = message });
        }

        public async Task<AnymateResponse> FinishRunAsync(string payload)
        {
            return await FinishRunAsync<AnymateResponse>(payload);
        }

        public async Task<TResponse> FinishRunAsync<TResponse>(string payload)
        {
            var endpoint = $"/api/FinishRun/";
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

        public async Task<AnymateResponse> FinishRunAsync(long runId, int? overwriteSecondsSaved = null,
            int? overwriteEntries = null)
        {
            return await FinishRunAsync(new AnymateFinishRun()
            { RunId = runId, OverwriteEntries = overwriteEntries, OverwriteSecondsSaved = overwriteSecondsSaved });
        }

        public async Task<AnymateRunResponse> StartOrGetRunAsync(string processKey)
        {
            return await StartOrGetRunAsync<AnymateRunResponse>(processKey);
        }

        public async Task<T> StartOrGetRunAsync<T>(string processKey)
        {
            var endpoint = $"/api/StartOrGetRun/{processKey}";
            var response = await CallApiGetAsync(endpoint);
            return JsonConvert.DeserializeObject<T>(response);
        }

        public async Task<T> OkToRunAsync<T>(string processKey)
        {
            var endpoint = $"/api/OkToRun/{processKey}";
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
            var endpoint = $"/api/GetRules/{processKey}";
            return await CallApiGetAsync(endpoint);
        }

        public async Task<string> TakeNextAsync(string processKey)
        {
            var endpoint = $"/api/TakeNext/{processKey}";
            return await CallApiGetAsync(endpoint);
        }

        public async Task<T> TakeNextAsync<T>(string processKey)
        {
            var jsonResult = await TakeNextAsync(processKey);
            var result = JsonConvert.DeserializeObject<T>(jsonResult);
            return result;
        }

        public async Task<AnymateCreateTaskResponse> CreateTaskAsync<T>(T newTask, string processKey)
        {
            return await CreateTaskAsync<AnymateCreateTaskResponse, T>(newTask, processKey);
        }

        public async Task<TResponse> CreateTaskAsync<TResponse, TModel>(TModel newTask, string processKey)
        {
            var payload = JsonConvert.SerializeObject(newTask);
            return await CreateTaskAsync<TResponse>(payload, processKey);
        }


        public async Task<TResponse> CreateTaskAsync<TResponse>(string payload, string processKey)
        {
            var endpoint = $"/api/CreateTask/{processKey}";
            var response = await CallApiPostAsync(endpoint, payload);
            return JsonConvert.DeserializeObject<TResponse>(response);
        }

        public async Task<AnymateCreateTasksResponse> CreateTasksAsync<T>(IEnumerable<T> newTasks, string processKey)
        {
            return await CreateTasksAsync<AnymateCreateTasksResponse, T>(newTasks, processKey);
        }

        public async Task<AnymateCreateTasksResponse> CreateTasksAsync(DataTable dt, string processKey)
        {
            var payload = JsonConvert.SerializeObject(dt);
            return await CreateTasksAsync<AnymateCreateTasksResponse>(payload, processKey);
        }


        public async Task<TResponse> CreateTasksAsync<TResponse, TModel>(IEnumerable<TModel> newTask, string processKey)
        {
            var payload = JsonConvert.SerializeObject(newTask);
            return await CreateTasksAsync<TResponse>(payload, processKey);
        }


        public async Task<TResponse> CreateTasksAsync<TResponse>(string payload, string processKey)
        {
            var endpoint = $"/api/CreateTasks/{processKey}";
            var response = await CallApiPostAsync(endpoint, payload);
            return JsonConvert.DeserializeObject<TResponse>(response);
        }

        public async Task<AnymateResponse> UpdateTaskAsync<T>(T updateTask)
        {
            return await UpdateTaskAsync<AnymateResponse, T>(updateTask);
        }

        public async Task<TResponse> UpdateTaskAsync<TResponse>(string payload)
        {
            var endpoint = $"/api/UpdateTask/";

            var response = await CallApiPostAsync(endpoint, payload);
            return JsonConvert.DeserializeObject<TResponse>(response);
        }

        public async Task<TResponse> UpdateTaskAsync<TResponse, TUpdate>(TUpdate updateTask)
        {
            var payload = JsonConvert.SerializeObject(updateTask);
            return await UpdateTaskAsync<TResponse>(payload);
        }

        public async Task<TResponse> CreateAndTakeTaskAsync<TResponse, TCreate>(TCreate newTask, string processKey)
        {
            var jsonResult = await CreateAndTakeTaskAsync<TCreate>(newTask, processKey);
            var response = JsonConvert.DeserializeObject<TResponse>(jsonResult);
            return response;
        }

        public async Task<string> CreateAndTakeTaskAsync<T>(T newTask, string processKey)
        {
            var payload = JsonConvert.SerializeObject(newTask);
            return await CreateAndTakeTaskAsync(payload, processKey);
        }

        public async Task<string> CreateAndTakeTaskAsync(string payload, string processKey)
        {
            var endpoint = $"/api/CreateAndTakeTask/{processKey}";
            var response = await CallApiPostAsync(endpoint, payload);
            return response;
        }

        public async Task<TResponse> CreateAndTakeTaskAsync<TResponse>(object newTask, string processKey)
        {
            var jsonResult = await CreateAndTakeTaskAsync(newTask, processKey);
            var response = JsonConvert.DeserializeObject<TResponse>(jsonResult);
            return response;
        }

        public async Task<T> ErrorAsync<T>(string payload)
        {
            var endpoint = $"/api/Error/";
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

        public async Task<AnymateResponse> ErrorAsync(long taskId, string reason = null, string comment = null,
            int? overwriteSecondsSaved = null, int? overwriteEntries = null)
        {
            var action = new AnymateTaskAction()
            {
                TaskId = taskId,
                Reason = reason,
                Comment = comment,
                OverwriteEntries = overwriteEntries,
                OverwriteSecondsSaved = overwriteSecondsSaved
            };
            return await ErrorAsync(action);
        }

        public async Task<AnymateResponse> RetryAsync(AnymateRetryTaskAction action)
        {
            return await RetryAsync<AnymateRetryTaskAction>(action);
        }

        public async Task<AnymateResponse> RetryAsync(string payload)
        {
            return await RetryAsync<AnymateResponse>(payload);
        }

        public async Task<TResponse> RetryAsync<TResponse>(string payload)
        {
            var endpoint = $"/api/Retry/";
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

        public async Task<AnymateResponse> RetryAsync(long taskId, string reason = null, string comment = null, DateTimeOffset? activationDate = null,
            int? overwriteSecondsSaved = null, int? overwriteEntries = null)
        {
            var action = new AnymateRetryTaskAction()
            {
                TaskId = taskId,
                Reason = reason,
                Comment = comment,
                OverwriteEntries = overwriteEntries,
                OverwriteSecondsSaved = overwriteSecondsSaved,
                ActivationDate = activationDate
            };
            return await RetryAsync(action);
        }

        public async Task<AnymateResponse> ManualAsync(string payload)
        {
            return await ManualAsync<AnymateResponse>(payload);
        }

        public async Task<TResponse> ManualAsync<TResponse>(string payload)
        {
            var endpoint = $"/api/Manual/";
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

        public async Task<AnymateResponse> ManualAsync(long taskId, string reason = null, string comment = null,
            int? overwriteSecondsSaved = null, int? overwriteEntries = null)
        {
            var action = new AnymateTaskAction()
            {
                TaskId = taskId,
                Reason = reason,
                Comment = comment,
                OverwriteEntries = overwriteEntries,
                OverwriteSecondsSaved = overwriteSecondsSaved
            };
            return await ManualAsync(action);
        }

        public async Task<AnymateResponse> SolvedAsync(string payload)
        {
            return await SolvedAsync<AnymateResponse>(payload);
        }

        public async Task<TResponse> SolvedAsync<TResponse>(string payload)
        {
            var endpoint = $"/api/Solved/";
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

        public async Task<AnymateResponse> SolvedAsync(long taskId, string reason = null, string comment = null,
            int? overwriteSecondsSaved = null, int? overwriteEntries = null)
        {
            var action = new AnymateTaskAction()
            {
                TaskId = taskId,
                Reason = reason,
                Comment = comment,
                OverwriteEntries = overwriteEntries,
                OverwriteSecondsSaved = overwriteSecondsSaved
            };
            return await SolvedAsync(action);
        }

        #endregion

        #region Sync Methods

        public AnymateResponse Failure(string payload)
        {
            return Failure<AnymateResponse>(payload);
        }

        public TResponse Failure<TResponse>(string payload)
        {
            var endpoint = $"/api/Failure/";
            var response = CallApiPost(endpoint, payload);
            return JsonConvert.DeserializeObject<TResponse>(response);
        }

        public AnymateResponse Failure(AnymateProcessFailure action)
        {
            return Failure<AnymateProcessFailure>(action);
        }

        public AnymateResponse Failure<T>(T action)
        {
            return Failure<AnymateResponse, T>(action);
        }

        public TResponse Failure<TResponse, TAction>(TAction action)
        {
            var payload = JsonConvert.SerializeObject(action);
            return Failure<TResponse>(payload);
        }

        public AnymateResponse Failure(string processKey, string message)
        {
            return Failure(new AnymateProcessFailure()
            { ProcessKey = processKey, Message = message });
        }


        public AnymateResponse FinishRun(string payload)
        {
            return FinishRun<AnymateResponse>(payload);
        }

        public TResponse FinishRun<TResponse>(string payload)
        {
            var endpoint = $"/api/FinishRun/";
            var response = CallApiPost(endpoint, payload);
            return JsonConvert.DeserializeObject<TResponse>(response);
        }

        public AnymateResponse FinishRun(AnymateFinishRun action)
        {
            return FinishRun<AnymateFinishRun>(action);
        }

        public AnymateResponse FinishRun<T>(T action)
        {
            return FinishRun<AnymateResponse, T>(action);
        }

        public TResponse FinishRun<TResponse, TAction>(TAction action)
        {
            var payload = JsonConvert.SerializeObject(action);
            return FinishRun<TResponse>(payload);
        }

        public AnymateResponse FinishRun(long runId, int? overwriteSecondsSaved = null,
            int? overwriteEntries = null)
        {
            return FinishRun(new AnymateFinishRun()
            { RunId = runId, OverwriteEntries = overwriteEntries, OverwriteSecondsSaved = overwriteSecondsSaved });
        }

        public AnymateRunResponse StartOrGetRun(string processKey)
        {
            return StartOrGetRun<AnymateRunResponse>(processKey);
        }

        public T StartOrGetRun<T>(string processKey)
        {
            var endpoint = $"/api/StartOrGetRun/{processKey}";
            var response = CallApiGet(endpoint);
            return JsonConvert.DeserializeObject<T>(response);
        }

        public T OkToRun<T>(string processKey)
        {
            var endpoint = $"/api/OkToRun/{processKey}";
            var response = CallApiGet(endpoint);
            return JsonConvert.DeserializeObject<T>(response);
        }

        public AnymateOkToRun OkToRun(string processKey)
        {
            return OkToRun<AnymateOkToRun>(processKey);
        }


        public T GetRules<T>(string processKey)
        {
            var jsonResult = GetRules(processKey);
            var result = JsonConvert.DeserializeObject<T>(jsonResult);
            return result;
        }

        public string GetRules(string processKey)
        {
            var endpoint = $"/api/GetRules/{processKey}";
            return CallApiGet(endpoint);
        }

        public string TakeNext(string processKey)
        {
            var endpoint = $"/api/TakeNext/{processKey}";
            return CallApiGet(endpoint);
        }

        public T TakeNext<T>(string processKey)
        {
            var jsonResult = TakeNext(processKey);
            var result = JsonConvert.DeserializeObject<T>(jsonResult);
            return result;
        }

        public AnymateCreateTaskResponse CreateTask<T>(T newTask, string processKey)
        {
            return CreateTask<AnymateCreateTaskResponse, T>(newTask, processKey);
        }

        public TResponse CreateTask<TResponse, TModel>(TModel newTask, string processKey)
        {
            var payload = JsonConvert.SerializeObject(newTask);
            return CreateTask<TResponse>(payload, processKey);
        }

        public TResponse CreateTask<TResponse>(string payload, string processKey)
        {
            var endpoint = $"/api/CreateTask/{processKey}";
            var response = CallApiPost(endpoint, payload);
            return JsonConvert.DeserializeObject<TResponse>(response);
        }


        public AnymateCreateTasksResponse CreateTasks<T>(IEnumerable<T> newTasks, string processKey)
        {
            return CreateTasks<AnymateCreateTasksResponse, T>(newTasks, processKey);
        }

        public AnymateCreateTasksResponse CreateTasks(DataTable dt, string processKey)
        {
            var payload = JsonConvert.SerializeObject(dt);
            return CreateTasks<AnymateCreateTasksResponse>(payload, processKey);
        }


        public TResponse CreateTasks<TResponse, TModel>(IEnumerable<TModel> newTask, string processKey)
        {
            var payload = JsonConvert.SerializeObject(newTask);
            return CreateTasks<TResponse>(payload, processKey);
        }


        public TResponse CreateTasks<TResponse>(string payload, string processKey)
        {
            var endpoint = $"/api/CreateTasks/{processKey}";
            var response = CallApiPost(endpoint, payload);
            return JsonConvert.DeserializeObject<TResponse>(response);
        }

        public AnymateResponse UpdateTask<T>(T updateTask)
        {
            return UpdateTask<AnymateResponse, T>(updateTask);
        }

        public TResponse UpdateTask<TResponse>(string payload)
        {
            var endpoint = $"/api/UpdateTask/";

            var response = CallApiPost(endpoint, payload);
            return JsonConvert.DeserializeObject<TResponse>(response);
        }

        public TResponse UpdateTask<TResponse, TUpdate>(TUpdate updateTask)
        {
            var payload = JsonConvert.SerializeObject(updateTask);
            return UpdateTask<TResponse>(payload);
        }

        public string CreateAndTakeTask<T>(T newTask, string processKey)
        {
            var payload = JsonConvert.SerializeObject(newTask);
            return CreateAndTakeTask(payload, processKey);
        }

        public string CreateAndTakeTask(string payload, string processKey)
        {
            var endpoint = $"/api/CreateAndTakeTask/{processKey}";
            var response = CallApiPost(endpoint, payload);
            return response;
        }

        public TResponse CreateAndTakeTask<TResponse, TCreate>(TCreate newTask, string processKey)
        {
            var jsonResult = CreateAndTakeTask(newTask, processKey);
            var response = JsonConvert.DeserializeObject<TResponse>(jsonResult);
            return response;
        }

        public TResponse CreateAndTakeTask<TResponse>(object newTask, string processKey)
        {
            var jsonResult = CreateAndTakeTask(newTask, processKey);
            var response = JsonConvert.DeserializeObject<TResponse>(jsonResult);
            return response;
        }

        public T Error<T>(string payload)
        {
            var endpoint = $"/api/Error/";
            var response = CallApiPost(endpoint, payload);
            return JsonConvert.DeserializeObject<T>(response);
        }

        public AnymateResponse Error(string payload)
        {
            return Error<AnymateResponse>(payload);
        }

        public AnymateResponse Error(AnymateTaskAction action)
        {
            return Error<AnymateTaskAction>(action);
        }

        public AnymateResponse Error<T>(T action)
        {
            return Error<AnymateResponse, T>(action);
        }

        public TResponse Error<TResponse, TAction>(TAction action)
        {
            var payload = JsonConvert.SerializeObject(action);
            return Error<TResponse>(payload);
        }

        public AnymateResponse Error(long taskId, string reason = null, string comment = null,
            int? overwriteSecondsSaved = null, int? overwriteEntries = null)
        {
            var action = new AnymateTaskAction()
            {
                TaskId = taskId,
                Reason = reason,
                Comment = comment,
                OverwriteEntries = overwriteEntries,
                OverwriteSecondsSaved = overwriteSecondsSaved
            };
            return Error(action);
        }

        public AnymateResponse Retry(AnymateRetryTaskAction action)
        {
            return Retry<AnymateRetryTaskAction>(action);
        }

        public AnymateResponse Retry(string payload)
        {
            return Retry<AnymateResponse>(payload);
        }

        public TResponse Retry<TResponse>(string payload)
        {
            var endpoint = $"/api/Retry/";
            var response = CallApiPost(endpoint, payload);
            return JsonConvert.DeserializeObject<TResponse>(response);
        }

        public TResponse Retry<TResponse, TAction>(TAction action)
        {
            var payload = JsonConvert.SerializeObject(action);
            return Retry<TResponse>(payload);
        }

        public AnymateResponse Retry<T>(T action)
        {
            return Retry<AnymateResponse, T>(action);
        }

        public AnymateResponse Retry(long taskId, string reason = null, string comment = null, DateTimeOffset? activationDate = null, int? overwriteSecondsSaved = null, int? overwriteEntries = null)
        {
            var action = new AnymateRetryTaskAction()
            {
                TaskId = taskId,
                Reason = reason,
                Comment = comment,
                OverwriteEntries = overwriteEntries,
                OverwriteSecondsSaved = overwriteSecondsSaved,
                ActivationDate = activationDate
            };
            return Retry(action);
        }

        public AnymateResponse Manual(string payload)
        {
            return Manual<AnymateResponse>(payload);
        }

        public TResponse Manual<TResponse>(string payload)
        {
            var endpoint = $"/api/Manual/";
            var response = CallApiPost(endpoint, payload);
            return JsonConvert.DeserializeObject<TResponse>(response);
        }

        public AnymateResponse Manual(AnymateTaskAction action)
        {
            return Manual<AnymateTaskAction>(action);
        }

        public AnymateResponse Manual<T>(T action)
        {
            return Manual<AnymateResponse, T>(action);
        }

        public TResponse Manual<TResponse, TAction>(TAction action)
        {
            var payload = JsonConvert.SerializeObject(action);
            return Manual<TResponse>(payload);
        }

        public AnymateResponse Manual(long taskId, string reason = null, string comment = null,
            int? overwriteSecondsSaved = null, int? overwriteEntries = null)
        {
            var action = new AnymateTaskAction()
            {
                TaskId = taskId,
                Reason = reason,
                Comment = comment,
                OverwriteEntries = overwriteEntries,
                OverwriteSecondsSaved = overwriteSecondsSaved
            };
            return Manual(action);
        }

        public AnymateResponse Solved(string payload)
        {
            return Solved<AnymateResponse>(payload);
        }

        public TResponse Solved<TResponse>(string payload)
        {
            var endpoint = $"/api/Solved/";
            var response = CallApiPost(endpoint, payload);
            return JsonConvert.DeserializeObject<TResponse>(response);
        }

        public AnymateResponse Solved(AnymateTaskAction action)
        {
            return Solved<AnymateTaskAction>(action);
        }

        public AnymateResponse Solved<T>(T action)
        {
            return Solved<AnymateResponse, T>(action);
        }

        public TResponse Solved<TResponse, TAction>(TAction action)
        {
            var payload = JsonConvert.SerializeObject(action);
            return Solved<TResponse>(payload);
        }

        public AnymateResponse Solved(long taskId, string reason = null, string comment = null,
            int? overwriteSecondsSaved = null, int? overwriteEntries = null)
        {
            var action = new AnymateTaskAction()
            {
                TaskId = taskId,
                Reason = reason,
                Comment = comment,
                OverwriteEntries = overwriteEntries,
                OverwriteSecondsSaved = overwriteSecondsSaved
            };
            return Solved(action);
        }

        #endregion
    }
}