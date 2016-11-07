using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Maintain
{
    public interface IMaintainServiceProvider : IExtension
    {
        string Description { get; }
        Task RunMaintainServiceAsync(IExecutionContext context);
    }

    public sealed class MaintainJobExtension : AgentService, IJobExtension
    {
        public Type ExtensionType => typeof(IJobExtension);
        public string HostType => "maintain";
        public IStep PrepareStep { get; private set; }
        public IStep FinallyStep { get; private set; }

        public MaintainJobExtension()
        {
            PrepareStep = new JobExtensionRunner(
                runAsync: MaintainAsync,
                alwaysRun: false,
                continueOnError: false,
                critical: true,
                displayName: "Maintain Job",
                enabled: true,
                @finally: false);
        }

        public string GetRootedPath(IExecutionContext context, string path)
        {
            return path;
        }

        public void ConvertLocalPath(IExecutionContext context, string localPath, out string repoName, out string sourcePath)
        {
            sourcePath = localPath;
            repoName = string.Empty;
        }

        private async Task MaintainAsync()
        {
            // Validate args.
            Trace.Entering();
            ArgUtil.NotNull(PrepareStep, nameof(PrepareStep));
            ArgUtil.NotNull(PrepareStep.ExecutionContext, nameof(PrepareStep.ExecutionContext));
            IExecutionContext executionContext = PrepareStep.ExecutionContext;

            var extensionManager = HostContext.GetService<IExtensionManager>();
            var maintainServiceProviders = extensionManager.GetExtensions<IMaintainServiceProvider>();

            if (maintainServiceProviders != null && maintainServiceProviders.Count > 0)
            {
                foreach (var maintainProvider in maintainServiceProviders)
                {
                    // all maintain operations should be best effort.
                    executionContext.Section($"Start maintain service: {maintainProvider.Description}");
                    try
                    {
                        await maintainProvider.RunMaintainServiceAsync(executionContext);
                    }
                    catch (Exception ex)
                    {
                        executionContext.Error(ex);
                    }

                    executionContext.Section($"Finish maintain service: {maintainProvider.Description}");
                }
            }
        }
    }
}