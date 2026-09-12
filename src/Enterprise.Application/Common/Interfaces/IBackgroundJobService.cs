using System.Linq.Expressions;

namespace Enterprise.Application.Common.Interfaces;

/// <summary>
/// Wraps Hangfire's <c>IBackgroundJobClient</c>/<c>IRecurringJobManager</c> so Application
/// depends on "schedule this work" rather than on the Hangfire package directly - swapping
/// the job runner later (e.g. for a cloud queue) only touches Infrastructure.
/// </summary>
public interface IBackgroundJobService
{
    string Enqueue<T>(Expression<Action<T>> methodCall);
    string Schedule<T>(Expression<Action<T>> methodCall, TimeSpan delay);
    void AddOrUpdateRecurring<T>(string jobId, Expression<Action<T>> methodCall, string cronExpression);
}
