using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
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


        /// <summary>
        /// This is the default way of using the AnymateClient. It assumes you are using the cloud version of Anymate.
        /// </summary>
        /// <remarks>
        /// No need for any further configuration, as the Anymate Client automatically will setup itself once you log in.
        /// </remarks>
        public AnymateService()
        {
            OnPremisesMode = false;
        }


        /// <summary>
        /// In order to run in OnPremisesMode, supply an Client Uri and Auth Uri
        /// </summary>
        /// <remarks>
        /// Supplying an Client Uri and Auth Uri will trigger the client to work in OnPremises mode, using the supplied url's instead of the cloud version of Anymate.
        /// </remarks>
        /// <param name="clientUri">The url for the anymate client installation. Url should just be the domain with http/https in front, similar to "https://customer.anymate.app"</param>
        /// <param name="authUri">The url for the anymate auth server installation. Url should just be the domain with http/https in front, similar to "https://customer.auth.anymate.app"</param>
        public AnymateService(string clientUri, string authUri)
        {
            OnPremisesMode = true;
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

        private Dictionary<string, string> GetFormDataRefreshAuth(AuthTokenRequest request)
        {
            var values = CreateFormData(request);
            values.Add("grant_type", "refresh_token");
            values.Add(nameof(request.refresh_token), request.refresh_token);
            return values;
        }

        private Dictionary<string, string> CreateFormData(AuthTokenRequest request)
        {
            var values = new Dictionary<string, string>();
            values.Add(nameof(request.client_id), request.client_id);
            values.Add(nameof(request.client_secret), request.client_secret);

            return values;
        }

        public AuthResponse RefreshAccessTokenIfNeeded(string access_token, string refresh_token, string customerKey, string secret)
        {
            var request = new AuthTokenRequest()
            {
                access_token = access_token,
                refresh_token = refresh_token,
                client_id = customerKey,
                client_secret = secret
            };
            return RefreshAccessTokenIfNeeded(request);
        }

        public AuthResponse RefreshAccessTokenIfNeeded(AuthTokenRequest request)
        {
            var access_token = request.access_token;
            if (!TokenValidator.RefreshNotNeeded(access_token))
            {
                return new AuthResponse()
                {
                    access_token = request.access_token,
                    refresh_token = request.refresh_token,
                    Succeeded = true
                };
            }
            else
            {
                if (string.IsNullOrWhiteSpace(request.client_id)) throw new ArgumentNullException("client_id was null.");
                if (string.IsNullOrWhiteSpace(request.client_secret)) throw new ArgumentNullException("client_secret was null.");
                if (string.IsNullOrEmpty(request.refresh_token)) throw new ArgumentNullException("refresh_token was null.");

                var token = GetAuthTokenRefreshFlow(request);
                return token;

            }
        }

        public AuthResponse GetOrRefreshAccessToken(string refresh_token, string customerKey, string secret, string access_token = null)
        {
            var request = new AuthTokenRequest()
            {
                access_token = access_token,
                username = null,
                password = null,
                refresh_token = refresh_token,
                client_id = customerKey,
                client_secret = secret
            };
            return GetOrRefreshAccessToken(request);
        }
        public AuthResponse GetOrRefreshAccessToken(string username, string password, string customerKey, string secret, string access_token = null)
        {
            var request = new AuthTokenRequest()
            {
                access_token = access_token,
                refresh_token = null,
                username = username,
                password = password,
                client_id = customerKey,
                client_secret = secret
            };
            return GetOrRefreshAccessToken(request);
        }

        public AuthResponse GetOrRefreshAccessToken(AuthTokenRequest request)
        {
            var access_token = request.access_token;
            if (string.IsNullOrWhiteSpace(access_token) || !TokenValidator.RefreshNotNeeded(access_token))
            {
                if (string.IsNullOrWhiteSpace(request.client_id))
                    throw new ArgumentNullException("client_id was null.");
                if (string.IsNullOrWhiteSpace(request.client_secret))
                    throw new ArgumentNullException("client_secret was null.");

                if (string.IsNullOrEmpty(request.refresh_token))
                {
                    if (string.IsNullOrWhiteSpace(request.password) || string.IsNullOrWhiteSpace(request.username))
                    {
                        throw new ArgumentNullException("Found no refresh token and either username or password were empty.");
                    }
                    var token = GetAuthTokenPasswordFlow(request);
                    return token;
                }
                else
                {
                    var token = GetAuthTokenRefreshFlow(request);
                    return token;
                }
            }
            else
            {
                return new AuthResponse()
                {
                    access_token = request.access_token,
                    refresh_token = request.refresh_token,
                    Succeeded = true
                };
            }
        }

        public AuthResponse GetAuthTokenPasswordFlow(AuthTokenRequest request)
        {
            var formData = GetFormDataPasswordAuth(request);
            return GetAuthToken(formData, request.client_id);
        }

        public AuthResponse GetAuthTokenRefreshFlow(AuthTokenRequest request)
        {
            var formData = GetFormDataRefreshAuth(request);
            return GetAuthToken(formData, request.client_id);
        }


        private AuthResponse GetAuthToken(Dictionary<string, string> formData, string customerKey)
        {
            var values = formData;

            var content = new FormUrlEncodedContent(values);
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.Timeout = TimeSpan.FromMinutes(5);
                using (HttpResponseMessage response = AsyncUtil.RunSync(() => client.PostAsync($"{GetAuthUrl(customerKey)}/connect/token", content)))
                {
                    using (HttpContent responseContent = response.Content)
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            string data = AsyncUtil.RunSync(() => responseContent.ReadAsStringAsync());
                            var json = JsonConvert.DeserializeObject<AuthResponse>(data);
                            json.HttpMessage = $"{response.StatusCode} - {response.ReasonPhrase}";
                            return json;
                        }
                        else
                        {

                            var result = AuthResponse.Failed;
                            result.HttpMessage = $"{response.StatusCode} - {response.ReasonPhrase}";
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
            var customerKey = jsonToken.Claims.FirstOrDefault(claim => claim.Type == "auth.anymate.app/CustomerKey").Value;

            if (string.IsNullOrWhiteSpace(customerKey))
                throw new Exception("Token invalid");

            return customerKey;
        }

        private string CallApiPost(string access_token, string endpoint, string jsonPayload)
        {
            var customerKey = GetCustomerKeyFromToken(access_token);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", access_token);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.Timeout = TimeSpan.FromMinutes(5);
                using (HttpResponseMessage response = AsyncUtil.RunSync(() => client.PostAsync(GetAnymateUrl(customerKey) + endpoint, content)))
                using (HttpContent responseContent = response.Content)
                {
                    string data = AsyncUtil.RunSync(() => responseContent.ReadAsStringAsync());
                    return data;
                }
            }
        }


        private string CallApiGet(string access_token, string endpoint)
        {
            var customerKey = GetCustomerKeyFromToken(access_token);
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", access_token);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.Timeout = TimeSpan.FromMinutes(5);
                using (HttpResponseMessage response = AsyncUtil.RunSync(() => client.GetAsync(GetAnymateUrl(customerKey) + endpoint)))
                using (HttpContent responseContent = response.Content)
                {
                    string data = AsyncUtil.RunSync(() => responseContent.ReadAsStringAsync());
                    return data;
                }
            }
        }




        public AnymateResponse Failure(string access_token, string payload)
        {
            return Failure<AnymateResponse>(access_token, payload);
        }

        public TResponse Failure<TResponse>(string access_token, string payload)
        {
            var endpoint = $"/apimate/Failure/";
            var response = CallApiPost(access_token, endpoint, payload);
            return JsonConvert.DeserializeObject<TResponse>(response);
        }

        public AnymateResponse Failure(string access_token, AnymateProcessFailure action)
        {
            return Failure<AnymateProcessFailure>(access_token, action);
        }

        public AnymateResponse Failure<T>(string access_token, T action)
        {
            return Failure<AnymateResponse, T>(access_token, action);
        }

        public TResponse Failure<TResponse, TAction>(string access_token, TAction action)
        {
            var payload = JsonConvert.SerializeObject(action);
            return Failure<TResponse>(access_token, payload);
        }









        public AnymateResponse FinishRun(string access_token, string payload)
        {
            return FinishRun<AnymateResponse>(access_token, payload);
        }

        public TResponse FinishRun<TResponse>(string access_token, string payload)
        {
            var endpoint = $"/apimate/FinishRun/";
            var response = CallApiPost(access_token, endpoint, payload);
            return JsonConvert.DeserializeObject<TResponse>(response);
        }

        public AnymateResponse FinishRun(string access_token, AnymateFinishRun action)
        {
            return FinishRun<AnymateFinishRun>(access_token, action);
        }

        public AnymateResponse FinishRun<T>(string access_token, T action)
        {
            return FinishRun<AnymateResponse, T>(access_token, action);
        }

        public TResponse FinishRun<TResponse, TAction>(string access_token, TAction action)
        {
            var payload = JsonConvert.SerializeObject(action);
            return FinishRun<TResponse>(access_token, payload);
        }

        public AnymateRunResponse StartOrGetRun(string access_token, string processKey)
        {
            return StartOrGetRun<AnymateRunResponse>(access_token, processKey);
        }

        public T StartOrGetRun<T>(string access_token, string processKey)
        {
            var endpoint = $"/apimate/StartOrGetRun/{processKey}";
            var response = CallApiGet(access_token, endpoint);
            return JsonConvert.DeserializeObject<T>(response);
        }

        public T OkToRun<T>(string access_token, string processKey)
        {
            var endpoint = $"/apimate/OkToRun/{processKey}";
            var response = CallApiGet(access_token, endpoint);
            return JsonConvert.DeserializeObject<T>(response);
        }

        public AnymateOkToRun OkToRun(string access_token, string processKey)
        {
            return OkToRun<AnymateOkToRun>(access_token, processKey);
        }

        public T GetVariables<T>(string access_token, string processKey)
        {
            var jsonResult = GetVariables(access_token, processKey);
            var result = JsonConvert.DeserializeObject<T>(jsonResult);
            return result;
        }

        public string GetVariables(string access_token, string processKey)
        {
            var endpoint = $"/apimate/GetRules/{processKey}";
            return CallApiGet(access_token, endpoint);
        }


        public T GetRules<T>(string access_token, string processKey)
        {
            var jsonResult = GetVariables(access_token, processKey);
            var result = JsonConvert.DeserializeObject<T>(jsonResult);
            return result;
        }

        public string GetRules(string access_token, string processKey)
        {
            var endpoint = $"/apimate/GetRules/{processKey}";
            return CallApiGet(access_token, endpoint);
        }
        public string TakeNext(string access_token, string processKey)
        {
            var endpoint = $"/apimate/TakeNext/{processKey}";
            return CallApiGet(access_token, endpoint);
        }
        public T TakeNext<T>(string access_token, string processKey)
        {
            var jsonResult = TakeNext(access_token, processKey);
            var result = JsonConvert.DeserializeObject<T>(jsonResult);
            return result;
        }

        public AnymateResponse CreateTask<T>(string access_token, T newTask, string processKey)
        {
            return CreateTask<AnymateResponse, T>(access_token, newTask, processKey);
        }

        public TResponse CreateTask<TResponse, TModel>(string access_token, TModel newTask, string processKey)
        {
            var payload = JsonConvert.SerializeObject(newTask);
            return CreateTask<TResponse>(access_token, payload, processKey);
        }

        public TResponse CreateTask<TResponse>(string access_token, string payload, string processKey)
        {
            var endpoint = $"/apimate/CreateTask/{processKey}";
            var response = CallApiPost(access_token, endpoint, payload);
            return JsonConvert.DeserializeObject<TResponse>(response);
        }

        public AnymateResponse UpdateTask<T>(string access_token, T updateTask)
        {
            return UpdateTask<AnymateResponse, T>(access_token, updateTask);
        }

        public TResponse UpdateTask<TResponse>(string access_token, string payload)
        {
            var endpoint = $"/apimate/UpdateTask/";
            
            var response = CallApiPost(access_token, endpoint, payload);
            return JsonConvert.DeserializeObject<TResponse>(response);
        }

        public TResponse UpdateTask<TResponse, TUpdate>(string access_token, TUpdate updateTask)
        {
            var payload = JsonConvert.SerializeObject(updateTask);
            return UpdateTask<TResponse>(access_token, payload);
        }

        public string CreateAndTakeTask<T>(string access_token, T newTask, string processKey)
        {
            var payload = JsonConvert.SerializeObject(newTask);
            return CreateAndTakeTask(access_token, payload, processKey);
        }
        public string CreateAndTakeTask(string access_token, string payload, string processKey)
        {
            var endpoint = $"/apimate/CreateAndTakeTask/{processKey}";
            var response = CallApiPost(access_token, endpoint, payload);
            return response;
        }

        public T Error<T>(string access_token, string payload)
        {
            var endpoint = $"/apimate/Error/";
            var response = CallApiPost(access_token, endpoint, payload);
            return JsonConvert.DeserializeObject<T>(response);
        }

        public AnymateResponse Error(string access_token, string payload)
        {
            return Error<AnymateResponse>(access_token, payload);
        }

        public AnymateResponse Error(string access_token, AnymateTaskAction action)
        {
            return Error<AnymateTaskAction>(access_token, action);
        }

        public AnymateResponse Error<T>(string access_token, T action)
        {
            return Error<AnymateResponse, T>(access_token, action);
        }

        public TResponse Error<TResponse, TAction>(string access_token, TAction action)
        {
            var payload = JsonConvert.SerializeObject(action);
            return Error<TResponse>(access_token, payload);
        }


        public AnymateResponse Retry(string access_token, AnymateTaskAction action)
        {
            return Retry<AnymateTaskAction>(access_token, action);
        }

        public AnymateResponse Retry(string access_token, string payload)
        {
            return Retry<AnymateResponse>(access_token, payload);
        }

        public TResponse Retry<TResponse>(string access_token, string payload)
        {
            var endpoint = $"/apimate/Retry/";
            var response = CallApiPost(access_token, endpoint, payload);
            return JsonConvert.DeserializeObject<TResponse>(response);
        }

        public TResponse Retry<TResponse, TAction>(string access_token, TAction action)
        {
            var payload = JsonConvert.SerializeObject(action);
            return Retry<TResponse>(access_token, payload);
        }

        public AnymateResponse Retry<T>(string access_token, T action)
        {
            return Retry<AnymateResponse, T>(access_token, action);
        }

        public AnymateResponse Manual(string access_token, string payload)
        {
            return Manual<AnymateResponse>(access_token, payload);
        }

        public TResponse Manual<TResponse>(string access_token, string payload)
        {
            var endpoint = $"/apimate/Manual/";
            var response = CallApiPost(access_token, endpoint, payload);
            return JsonConvert.DeserializeObject<TResponse>(response);
        }

        public AnymateResponse Manual(string access_token, AnymateTaskAction action)
        {
            return Manual<AnymateTaskAction>(access_token, action);
        }
        public AnymateResponse Manual<T>(string access_token, T action)
        {
            return Manual<AnymateResponse, T>(access_token, action);
        }

        public TResponse Manual<TResponse, TAction>(string access_token, TAction action)
        {
            var payload = JsonConvert.SerializeObject(action);
            return Manual<TResponse>(access_token, payload);
        }

        public AnymateResponse Solved(string access_token, string payload)
        {
            return Solved<AnymateResponse>(access_token, payload);
        }

        public TResponse Solved<TResponse>(string access_token, string payload)
        {
            var endpoint = $"/apimate/Solved/";
            var response = CallApiPost(access_token, endpoint, payload);
            return JsonConvert.DeserializeObject<TResponse>(response);
        }

        public AnymateResponse Solved(string access_token, AnymateTaskAction action)
        {
            return Solved<AnymateTaskAction>(access_token, action);
        }

        public AnymateResponse Solved<T>(string access_token, T action)
        {
            return Solved<AnymateResponse, T>(access_token, action);
        }

        public TResponse Solved<TResponse, TAction>(string access_token, TAction action)
        {
            var payload = JsonConvert.SerializeObject(action);
            return Solved<TResponse>(access_token, payload);
        }

    }
}
