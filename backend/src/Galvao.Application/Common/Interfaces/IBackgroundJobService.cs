using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Galvao.Application.Common.Interfaces;

public interface IBackgroundJobService
{
    // Fire-and-forget
    /// <summary>
    /// Adds a one-time job to the queue.
    /// </summary>
    /// <typeparam name="T">The type of the job.</typeparam>
    /// <param name="methodCall">The method call to enqueue.</param>
    void Enqueue<T>(Expression<Func<T, Task>> methodCall);

    // Delayed
    /// <summary>
    /// Schedules a one-time job to run at a specific date and time.
    /// </summary>
    /// <typeparam name="T">The type of the job.</typeparam>
    /// <param name="methodCall">The method call to schedule.</param>
    /// <param name="enqueueAt">The date and time to run the job.</param>
    void Schedule<T>(Expression<Func<T, Task>> methodCall, DateTimeOffset enqueueAt);

    // DelayedWithParams
        /// <summary>
    /// Schedules a one-time job to run after a specific delay.
    /// </summary>
    /// <typeparam name="T">The type of the job.</typeparam>
    /// <param name="methodCall">The method call to schedule.</param>
    /// <param name="delay">The delay before running the job.</param>
    void Schedule<T>(Expression<Func<T, Task>> methodCall, TimeSpan delay);

    // Recurring
    /// <summary>
    /// Adds or updates a recurring job.
    /// </summary>
    /// <typeparam name="T">The type of the job.</typeparam>
    /// <param name="recurringJobId">The ID of the recurring job.</param>
    /// <param name="methodCall">The method call to schedule.</param>
    /// <param name="cronExpression">The CRON expression for the recurring job.</param>
    void AddOrUpdate<T>(string recurringJobId, Expression<Func<T, Task>> methodCall, string cronExpression);
}
