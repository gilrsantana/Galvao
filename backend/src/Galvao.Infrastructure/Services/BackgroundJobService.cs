using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Galvao.Application.Common.Interfaces;
using Hangfire;

namespace Galvao.Infrastructure.Services;

/// <summary>
/// A service for managing background jobs using Hangfire.
/// </summary>
public class BackgroundJobService : IBackgroundJobService
{
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IRecurringJobManager _recurringJobManager;

    public BackgroundJobService(IBackgroundJobClient backgroundJobClient, IRecurringJobManager recurringJobManager)
    {
        _backgroundJobClient = backgroundJobClient;
        _recurringJobManager = recurringJobManager;
    }

    /// <inheritdoc />
    public void AddOrUpdate<T>(string recurringJobId, Expression<Func<T, Task>> methodCall, string cronExpression)
    {
        _recurringJobManager.AddOrUpdate<T>(recurringJobId, methodCall, cronExpression);
    }

    /// <inheritdoc />
    public void Enqueue<T>(Expression<Func<T, Task>> methodCall)
    {
        _backgroundJobClient.Enqueue<T>(methodCall);
    }

    /// <inheritdoc />
    public void Schedule<T>(Expression<Func<T, Task>> methodCall, DateTimeOffset enqueueAt)
    {
        _backgroundJobClient.Schedule<T>(methodCall, enqueueAt);
    }

    /// <inheritdoc />
    public void Schedule<T>(Expression<Func<T, Task>> methodCall, TimeSpan delay)
    {
        _backgroundJobClient.Schedule<T>(methodCall, delay);
    }
}
