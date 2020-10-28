using Anymate.Models;
using System.Threading.Tasks;

namespace Anymate
{
    public interface IAnymateService
    {
        string AccessToken { get; set; }
        bool HasAuthCredentials { get; }
        Task<string> CreateAndTakeTaskAsync(string payload, string processKey);
        Task<string> CreateAndTakeTaskAsync<T>(T newTask, string processKey);
        Task<AnymateResponse> CreateTaskAsync<T>(T newTask, string processKey);
        Task<TResponse> CreateTaskAsync<TResponse, TModel>(TModel newTask, string processKey);
        Task<TResponse> CreateTaskAsync<TResponse>(string payload, string processKey);
        Task<AnymateResponse> ErrorAsync(AnymateTaskAction action);
        Task<AnymateResponse> ErrorAsync(string payload);
        Task<T> ErrorAsync<T>(string payload);
        Task<AnymateResponse> ErrorAsync<T>(T action);
        Task<TResponse> ErrorAsync<TResponse, TAction>(TAction action);
        Task<AnymateResponse> FailureAsync(AnymateProcessFailure action);
        Task<AnymateResponse> FailureAsync(string payload);
        Task<AnymateResponse> FailureAsync<T>(T action);
        Task<TResponse> FailureAsync<TResponse, TAction>(TAction action);
        Task<TResponse> FailureAsync<TResponse>(string payload);
        Task<AnymateResponse> FinishRunAsync(AnymateFinishRun action);
        Task<AnymateResponse> FinishRunAsync(string payload);
        Task<AnymateResponse> FinishRunAsync<T>(T action);
        Task<TResponse> FinishRunAsync<TResponse, TAction>(TAction action);
        Task<TResponse> FinishRunAsync<TResponse>(string payload);
        Task<string> GetRulesAsync(string processKey);
        Task<T> GetRulesAsync<T>(string processKey);
        Task<AnymateResponse> ManualAsync(AnymateTaskAction action);
        Task<AnymateResponse> ManualAsync(string payload);
        Task<AnymateResponse> ManualAsync<T>(T action);
        Task<TResponse> ManualAsync<TResponse, TAction>(TAction action);
        Task<TResponse> ManualAsync<TResponse>(string payload);
        Task<AnymateOkToRun> OkToRunAsync(string processKey);
        Task<T> OkToRunAsync<T>(string processKey);
        Task<AnymateResponse> RetryAsync(AnymateTaskAction action);
        Task<AnymateResponse> RetryAsync(string payload);
        Task<AnymateResponse> RetryAsync<T>(T action);
        Task<TResponse> RetryAsync<TResponse, TAction>(TAction action);
        Task<TResponse> RetryAsync<TResponse>(string payload);
        Task<AnymateResponse> SolvedAsync(AnymateTaskAction action);
        Task<AnymateResponse> SolvedAsync(string payload);
        Task<AnymateResponse> SolvedAsync<T>(T action);
        Task<TResponse> SolvedAsync<TResponse, TAction>(TAction action);
        Task<TResponse> SolvedAsync<TResponse>(string payload);
        Task<AnymateRunResponse> StartOrGetRunAsync(string processKey);
        Task<T> StartOrGetRunAsync<T>(string processKey);
        Task<string> TakeNextAsync(string processKey);
        Task<T> TakeNextAsync<T>(string processKey);
        Task<AnymateResponse> UpdateTaskAsync<T>(T updateTask);
        Task<TResponse> UpdateTaskAsync<TResponse, TUpdate>(TUpdate updateTask);
        Task<TResponse> UpdateTaskAsync<TResponse>(string payload);
    }
}