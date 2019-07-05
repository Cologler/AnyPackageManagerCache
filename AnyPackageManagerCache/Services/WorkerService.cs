using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AnyPackageManagerCache.Services
{
    public class WorkerService
    {
        private readonly ILogger _logger;
        private readonly IServiceProvider _serviceProvider;

        public WorkerService(ILogger<WorkerService> logger, IServiceProvider serviceProvider)
        {
            this._logger = logger;
            this._serviceProvider = serviceProvider;
        }

        public void AddJob(Action<IServiceProvider> work)
        {
            using (var scope = this._serviceProvider.CreateScope())
            {
                try
                {
                    work(scope.ServiceProvider);
                }
                catch (Exception e)
                {
                    this._logger.LogError(e, e.Message);
                }
            }
        }

        public async void AddJob(Func<IServiceProvider, Task> work)
        {
            using (var scope = this._serviceProvider.CreateScope())
            {
                try
                {
                    await work(scope.ServiceProvider);
                }
                catch (Exception e)
                {
                    this._logger.LogError(e, e.Message);
                }
            }
        }

        public void AddJob(Action<IServiceProvider, ILogger> work)
        {
            using (var scope = this._serviceProvider.CreateScope())
            {
                try
                {
                    work(scope.ServiceProvider, this._logger);
                }
                catch (Exception e)
                {
                    this._logger.LogError(e, e.Message);
                }
            }
        }

        public async void AddJob(Func<IServiceProvider, ILogger, Task> work)
        {
            using (var scope = this._serviceProvider.CreateScope())
            {
                try
                {
                    await work(scope.ServiceProvider, this._logger);
                }
                catch (Exception e)
                {
                    this._logger.LogError(e, e.Message);
                }
            }
        }
    }
}
