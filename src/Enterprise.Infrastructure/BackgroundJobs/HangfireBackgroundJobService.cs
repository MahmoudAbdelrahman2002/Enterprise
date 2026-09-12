using System.Linq.Expressions;
using Enterprise.Application.Common.Interfaces;
using Hangfire;

namespace Enterprise.Infrastructure.BackgroundJobs;

public sealed class HangfireBackgroundJobService(
    IBackgroundJobClient backgroundJobClient,
    IRecurringJobManager recurringJobManager) : IBackgroundJobService
{
    public string Enqueue<T>(Expression<Action<T>> methodCall) => backgroundJobClient.Enqueue(methodCall);

    public string Schedule<T>(Expression<Action<T>> methodCall, TimeSpan delay) =>
        backgroundJobClient.Schedule(methodCall, delay);

    public void AddOrUpdateRecurring<T>(string jobId, Expression<Action<T>> methodCall, string cronExpression) =>
        recurringJobManager.AddOrUpdate(jobId, methodCall, cronExpression);
}
