using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Galvao.Application.Common.Interfaces;

public interface IBackgroundJobService
{
    void Enqueue<T>(Expression<Func<T, Task>> methodCall);
}
