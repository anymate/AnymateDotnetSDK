using Anymate.Models;

namespace Anymate
{
    public interface IAnymateService
    {
        AuthResponse RefreshAccessTokenIfNeeded(string access_token, string refresh_token, string customerKey, string secret);
        AuthResponse RefreshAccessTokenIfNeeded(AuthTokenRequest request);
        AuthResponse GetOrRefreshAccessToken(string refresh_token, string customerKey, string secret, string access_token = null);
        AuthResponse GetOrRefreshAccessToken(string username, string password, string customerKey, string secret, string access_token = null);
        AuthResponse GetOrRefreshAccessToken(AuthTokenRequest request);
        AuthResponse GetAuthTokenPasswordFlow(AuthTokenRequest request);
        AuthResponse GetAuthTokenRefreshFlow(AuthTokenRequest request);
        AnymateResponse FinishRun(string access_token, string payload);
        TResponse FinishRun<TResponse>(string access_token, string payload);
        AnymateResponse FinishRun(string access_token, AnymateFinishRun action);
        AnymateResponse FinishRun<T>(string access_token, T action);
        TResponse FinishRun<TResponse, TAction>(string access_token, TAction action);
        AnymateRunResponse StartOrGetRun(string access_token, string processKey);
        T StartOrGetRun<T>(string access_token, string processKey);
        T OkToRun<T>(string access_token, string processKey);
        AnymateOkToRun OkToRun(string access_token, string processKey);
        T GetVariables<T>(string access_token, string processKey);
        string GetVariables(string access_token, string processKey);
        string TakeNext(string access_token, string processKey);
        T TakeNext<T>(string access_token, string processKey);
        AnymateResponse CreateTask<T>(string access_token, T newTask, string processKey);
        TResponse CreateTask<TResponse, TModel>(string access_token, TModel newTask, string processKey);
        TResponse CreateTask<TResponse>(string access_token, string payload, string processKey);
        AnymateResponse UpdateTask<T>(string access_token, T updateTask);
        TResponse UpdateTask<TResponse, TUpdate>(string access_token, TUpdate updateTask);
        TResponse UpdateTask<TResponse>(string access_token, string payload);
        string CreateAndTakeTask<T>(string access_token, T newTask, string processKey);
        string CreateAndTakeTask(string access_token, string payload, string processKey);
        T Error<T>(string access_token, string payload);
        AnymateResponse Error(string access_token, string payload);
        AnymateResponse Error(string access_token, AnymateTaskAction action);
        AnymateResponse Error<T>(string access_token, T action);
        TResponse Error<TResponse, TAction>(string access_token, TAction action);
        AnymateResponse Retry(string access_token, AnymateTaskAction action);
        AnymateResponse Retry(string access_token, string payload);
        TResponse Retry<TResponse>(string access_token, string payload);
        TResponse Retry<TResponse, TAction>(string access_token, TAction action);
        AnymateResponse Retry<T>(string access_token, T action);
        AnymateResponse Manual(string access_token, string payload);
        TResponse Manual<TResponse>(string access_token, string payload);
        AnymateResponse Manual(string access_token, AnymateTaskAction action);
        AnymateResponse Manual<T>(string access_token, T action);
        TResponse Manual<TResponse, TAction>(string access_token, TAction action);
        AnymateResponse Solved(string access_token, string payload);
        TResponse Solved<TResponse>(string access_token, string payload);
        AnymateResponse Solved(string access_token, AnymateTaskAction action);
        AnymateResponse Solved<T>(string access_token, T action);
        TResponse Solved<TResponse, TAction>(string access_token, TAction action);
        AnymateResponse Failure(string access_token, string payload);
        TResponse Failure<TResponse>(string access_token, string payload);
        AnymateResponse Failure(string access_token, AnymateProcessFailure action);
        AnymateResponse Failure<T>(string access_token, T action);
        TResponse Failure<TResponse, TAction>(string access_token, TAction action);
        T GetRules<T>(string access_token, string processKey);
        string GetRules(string access_token, string processKey);
    }
}