using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Galvao.Application.Common.Interfaces;
using Hangfire;

namespace Galvao.Infrastructure.Services;

public class BackgroundJobService : IBackgroundJobService
{
    private readonly IBackgroundJobClient _backgroundJobClient;

    public BackgroundJobService(IBackgroundJobClient backgroundJobClient)
    {
        _backgroundJobClient = backgroundJobClient;
    }

    public void Enqueue<T>(Expression<Func<T, Task>> methodCall)
    {
        _backgroundJobClient.Enqueue<T>(methodCall);
    }
}
