using Quartz;
using Quartz.Spi;

namespace FrietBot.Jobs
{
    public class ServiceProviderJobFactory : IJobFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public ServiceProviderJobFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
        {
            // Get the job type
            Type jobType = bundle.JobDetail.JobType;
            
            try
            {
                // Create a new scope for each job execution
                var scope = _serviceProvider.CreateScope();
                
                // Store the scope in the scheduler context
                scheduler.Context.TryAdd($"{jobType.FullName}-scope", scope);
                
                // Get the job from the scope
                var job = scope.ServiceProvider.GetRequiredService(jobType) as IJob;
                
                if (job == null)
                {
                    throw new SchedulerException(
                        $"Failed to instantiate job '{jobType.Name}': Service provider returned null");
                }
                
                return job;
            }
            catch (Exception ex) when (!(ex is SchedulerException))
            {
                throw new SchedulerException(
                    $"Problem instantiating job '{jobType.Name}' from the service provider", ex);
            }
        }

        public void ReturnJob(IJob job)
        {
            // Find and dispose the scope we created
            var disposable = job as IDisposable;
            disposable?.Dispose();
        }
    }
}