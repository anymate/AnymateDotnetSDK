
using System;
using System.Threading.Tasks;

namespace Anymate
{
    public interface IAnymateService
    {
        string AccessToken { get; set; }
        bool HasAuthCredentials { get; }
        Task<AnymateResponse> FailureAsync(string payload);
        Task<TResponse> FailureAsync<TResponse>(string payload);
        Task<AnymateResponse> FailureAsync(AnymateProcessFailure action);
        Task<AnymateResponse> FailureAsync<T>(T action);
        Task<TResponse> FailureAsync<TResponse, TAction>(TAction action);
        Task<AnymateResponse> FailureAsync(string processKey, string message);
        Task<AnymateResponse> FinishRunAsync(string payload);
        Task<TResponse> FinishRunAsync<TResponse>(string payload);
        Task<AnymateResponse> FinishRunAsync(AnymateFinishRun action);
        Task<AnymateResponse> FinishRunAsync<T>(T action);
        Task<TResponse> FinishRunAsync<TResponse, TAction>(TAction action);

        Task<AnymateResponse> FinishRunAsync(long runId, int? overwriteSecondsSaved = null,
            int? overwriteEntries = null);

        Task<AnymateRunResponse> StartOrGetRunAsync(string processKey);
        Task<T> StartOrGetRunAsync<T>(string processKey);
        Task<T> OkToRunAsync<T>(string processKey);
        Task<AnymateOkToRun> OkToRunAsync(string processKey);
        Task<T> GetRulesAsync<T>(string processKey);
        Task<string> GetRulesAsync(string processKey);
        Task<string> TakeNextAsync(string processKey);
        Task<T> TakeNextAsync<T>(string processKey);
        Task<AnymateCreateTaskResponse> CreateTaskAsync<T>(T newTask, string processKey);
        Task<TResponse> CreateTaskAsync<TResponse, TModel>(TModel newTask, string processKey);
        Task<TResponse> CreateTaskAsync<TResponse>(string payload, string processKey);
        Task<AnymateResponse> UpdateTaskAsync<T>(T updateTask);
        Task<TResponse> UpdateTaskAsync<TResponse>(string payload);
        Task<TResponse> UpdateTaskAsync<TResponse, TUpdate>(TUpdate updateTask);
        Task<TResponse> CreateAndTakeTaskAsync<TResponse, TCreate>(TCreate newTask, string processKey);
        Task<string> CreateAndTakeTaskAsync<T>(T newTask, string processKey);
        Task<string> CreateAndTakeTaskAsync(string payload, string processKey);
        Task<TResponse> CreateAndTakeTaskAsync<TResponse>(object newTask, string processKey);
        Task<T> ErrorAsync<T>(string payload);
        Task<AnymateResponse> ErrorAsync(string payload);
        Task<AnymateResponse> ErrorAsync(AnymateTaskAction action);
        Task<AnymateResponse> ErrorAsync<T>(T action);
        Task<TResponse> ErrorAsync<TResponse, TAction>(TAction action);

        Task<AnymateResponse> ErrorAsync(long taskId, string reason = null, string comment = null,
            int? overwriteSecondsSaved = null, int? overwriteEntries = null);

        Task<AnymateResponse> RetryAsync(AnymateRetryTaskAction action);
        Task<AnymateResponse> RetryAsync(string payload);
        Task<TResponse> RetryAsync<TResponse>(string payload);
        Task<TResponse> RetryAsync<TResponse, TAction>(TAction action);
        Task<AnymateResponse> RetryAsync<T>(T action);

        Task<AnymateResponse> RetryAsync(long taskId, string reason = null, string comment = null, DateTimeOffset? activationDate = null, int? overwriteSecondsSaved = null, int? overwriteEntries = null);

        Task<AnymateResponse> ManualAsync(string payload);
        Task<TResponse> ManualAsync<TResponse>(string payload);
        Task<AnymateResponse> ManualAsync(AnymateTaskAction action);
        Task<AnymateResponse> ManualAsync<T>(T action);
        Task<TResponse> ManualAsync<TResponse, TAction>(TAction action);

        Task<AnymateResponse> ManualAsync(long taskId, string reason = null, string comment = null,
            int? overwriteSecondsSaved = null, int? overwriteEntries = null);

        Task<AnymateResponse> SolvedAsync(string payload);
        Task<TResponse> SolvedAsync<TResponse>(string payload);
        Task<AnymateResponse> SolvedAsync(AnymateTaskAction action);
        Task<AnymateResponse> SolvedAsync<T>(T action);
        Task<TResponse> SolvedAsync<TResponse, TAction>(TAction action);

        Task<AnymateResponse> SolvedAsync(long taskId, string reason = null, string comment = null,
            int? overwriteSecondsSaved = null, int? overwriteEntries = null);

        AnymateResponse Failure(string payload);
        TResponse Failure<TResponse>(string payload);
        AnymateResponse Failure(AnymateProcessFailure action);
        AnymateResponse Failure<T>(T action);
        TResponse Failure<TResponse, TAction>(TAction action);
        AnymateResponse Failure(string processKey, string message);
        AnymateResponse FinishRun(string payload);
        TResponse FinishRun<TResponse>(string payload);
        AnymateResponse FinishRun(AnymateFinishRun action);
        AnymateResponse FinishRun<T>(T action);
        TResponse FinishRun<TResponse, TAction>(TAction action);

        AnymateResponse FinishRun(long runId, int? overwriteSecondsSaved = null,
            int? overwriteEntries = null);

        AnymateRunResponse StartOrGetRun(string processKey);
        T StartOrGetRun<T>(string processKey);
        T OkToRun<T>(string processKey);
        AnymateOkToRun OkToRun(string processKey);
        T GetRules<T>(string processKey);
        string GetRules(string processKey);
        string TakeNext(string processKey);
        T TakeNext<T>(string processKey);
        AnymateCreateTaskResponse CreateTask<T>(T newTask, string processKey);
        TResponse CreateTask<TResponse, TModel>(TModel newTask, string processKey);
        TResponse CreateTask<TResponse>(string payload, string processKey);
        AnymateResponse UpdateTask<T>(T updateTask);
        TResponse UpdateTask<TResponse>(string payload);
        TResponse UpdateTask<TResponse, TUpdate>(TUpdate updateTask);
        string CreateAndTakeTask<T>(T newTask, string processKey);
        string CreateAndTakeTask(string payload, string processKey);
        TResponse CreateAndTakeTask<TResponse, TCreate>(TCreate newTask, string processKey);
        TResponse CreateAndTakeTask<TResponse>(object newTask, string processKey);
        T Error<T>(string payload);
        AnymateResponse Error(string payload);
        AnymateResponse Error(AnymateTaskAction action);
        AnymateResponse Error<T>(T action);
        TResponse Error<TResponse, TAction>(TAction action);

        AnymateResponse Error(long taskId, string reason = null, string comment = null,
            int? overwriteSecondsSaved = null, int? overwriteEntries = null);

        AnymateResponse Retry(AnymateRetryTaskAction action);
        AnymateResponse Retry(string payload);
        TResponse Retry<TResponse>(string payload);
        TResponse Retry<TResponse, TAction>(TAction action);
        AnymateResponse Retry<T>(T action);

        AnymateResponse Retry(long taskId, string reason = null, string comment = null, DateTimeOffset? activationDate = null, int? overwriteSecondsSaved = null, int? overwriteEntries = null);

        AnymateResponse Manual(string payload);
        TResponse Manual<TResponse>(string payload);
        AnymateResponse Manual(AnymateTaskAction action);
        AnymateResponse Manual<T>(T action);
        TResponse Manual<TResponse, TAction>(TAction action);

        AnymateResponse Manual(long taskId, string reason = null, string comment = null,
            int? overwriteSecondsSaved = null, int? overwriteEntries = null);

        AnymateResponse Solved(string payload);
        TResponse Solved<TResponse>(string payload);
        AnymateResponse Solved(AnymateTaskAction action);
        AnymateResponse Solved<T>(T action);
        TResponse Solved<TResponse, TAction>(TAction action);

        AnymateResponse Solved(long taskId, string reason = null, string comment = null,
            int? overwriteSecondsSaved = null, int? overwriteEntries = null);
    }
}