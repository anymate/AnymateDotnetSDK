using Anymate.Models;

namespace Anymate
{
    public interface IAnymateService
    {
        string AccessToken { get; set; }
        bool HasAuthCredentials { get; }
        AnymateResponse FinishRun(string payload);
        TResponse FinishRun<TResponse>(string payload);
        AnymateResponse FinishRun(AnymateFinishRun action);
        AnymateResponse FinishRun<T>(T action);
        TResponse FinishRun<TResponse, TAction>(TAction action);
        AnymateRunResponse StartOrGetRun(string processKey);
        T StartOrGetRun<T>(string processKey);
        T OkToRun<T>(string processKey);
        AnymateOkToRun OkToRun(string processKey);
        T GetVariables<T>(string processKey);
        string GetVariables(string processKey);
        string TakeNext(string processKey);
        T TakeNext<T>(string processKey);
        AnymateResponse CreateTask<T>(T newTask, string processKey);
        TResponse CreateTask<TResponse, TModel>(TModel newTask, string processKey);
        TResponse CreateTask<TResponse>(string payload, string processKey);
        AnymateResponse UpdateTask<T>(T updateTask);
        TResponse UpdateTask<TResponse, TUpdate>(TUpdate updateTask);
        TResponse UpdateTask<TResponse>(string payload);
        string CreateAndTakeTask<T>(T newTask, string processKey);
        string CreateAndTakeTask(string payload, string processKey);
        T Error<T>(string payload);
        AnymateResponse Error(string payload);
        AnymateResponse Error(AnymateTaskAction action);
        AnymateResponse Error<T>(T action);
        TResponse Error<TResponse, TAction>(TAction action);
        AnymateResponse Retry(AnymateTaskAction action);
        AnymateResponse Retry(string payload);
        TResponse Retry<TResponse>(string payload);
        TResponse Retry<TResponse, TAction>(TAction action);
        AnymateResponse Retry<T>(T action);
        AnymateResponse Manual(string payload);
        TResponse Manual<TResponse>(string payload);
        AnymateResponse Manual(AnymateTaskAction action);
        AnymateResponse Manual<T>(T action);
        TResponse Manual<TResponse, TAction>(TAction action);
        AnymateResponse Solved(string payload);
        TResponse Solved<TResponse>(string payload);
        AnymateResponse Solved(AnymateTaskAction action);
        AnymateResponse Solved<T>(T action);
        TResponse Solved<TResponse, TAction>(TAction action);
        AnymateResponse Failure(string payload);
        TResponse Failure<TResponse>(string payload);
        AnymateResponse Failure(AnymateProcessFailure action);
        AnymateResponse Failure<T>(T action);
        TResponse Failure<TResponse, TAction>(TAction action);
        T GetRules<T>(string processKey);
        string GetRules(string processKey);
    }
}