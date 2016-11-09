using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Threading.Tasks;
using System.IO;
using System.Threading;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Maintain
{
    public interface IMaintenanceServiceProvider : IExtension
    {
        string Description { get; }
        Task MaintainAsync(IExecutionContext context);
    }

    public sealed class MaintenanceJobExtension : AgentService, IJobExtension
    {
        public Type ExtensionType => typeof(IJobExtension);
        public string HostType => "maintenance";
        public IStep PrepareStep { get; private set; }
        public IStep FinallyStep { get; private set; }

        public MaintenanceJobExtension()
        {
            PrepareStep = new JobExtensionRunner(
                runAsync: PrepareAsync,
                alwaysRun: false,
                continueOnError: false,
                critical: true,
                displayName: "Run Maintenance Job",
                enabled: true,
                @finally: false);

            FinallyStep = new JobExtensionRunner(
                runAsync: FinallyAsync,
                alwaysRun: false,
                continueOnError: false,
                critical: false,
                displayName: "Report Agent Status",
                enabled: true,
                @finally: true);
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

        private async Task PrepareAsync()
        {
            // Validate args.
            Trace.Entering();
            ArgUtil.NotNull(PrepareStep, nameof(PrepareStep));
            ArgUtil.NotNull(PrepareStep.ExecutionContext, nameof(PrepareStep.ExecutionContext));
            IExecutionContext executionContext = PrepareStep.ExecutionContext;

            var extensionManager = HostContext.GetService<IExtensionManager>();
            var maintainServiceProviders = extensionManager.GetExtensions<IMaintenanceServiceProvider>();

            if (maintainServiceProviders != null && maintainServiceProviders.Count > 0)
            {
                foreach (var maintainProvider in maintainServiceProviders)
                {
                    // all maintain operations should be best effort.
                    executionContext.Section($"Start maintain service: {maintainProvider.Description}");
                    try
                    {
                        await maintainProvider.MaintainAsync(executionContext);
                    }
                    catch (Exception ex)
                    {
                        executionContext.Error(ex);
                    }

                    executionContext.Section($"Finish maintain service: {maintainProvider.Description}");
                }
            }
        }

        private async Task FinallyAsync()
        {
            // Validate args.
            Trace.Entering();
            ArgUtil.NotNull(FinallyStep, nameof(FinallyStep));
            ArgUtil.NotNull(FinallyStep.ExecutionContext, nameof(FinallyStep.ExecutionContext));
            IExecutionContext executionContext = FinallyStep.ExecutionContext;

            string workDirectory = HostContext.GetDirectory(WellKnownDirectory.Work);
            string pathRoot = Path.GetPathRoot(workDirectory);

            DriveInfo driveInfo = new DriveInfo(pathRoot);
            executionContext.Output($"AvailableFreeSpace: {driveInfo.AvailableFreeSpace}");

            long workDirectorySize = await CalculateDirectorySize(executionContext, workDirectory);
            executionContext.Output($"'{workDirectory}': {workDirectorySize}");
        }

        private async Task<long> CalculateDirectorySize(IExecutionContext context, string directoryPath)
        {
            long directorySize = 0;
            fileProcessCounter = 0;
            DirectoryInfo workDirectoryInfo = new DirectoryInfo(directoryPath);
            CancellationTokenSource progressReportCancellationToken = new CancellationTokenSource();
            Task progressReportTask = FileScanReport(context, progressReportCancellationToken.Token);

            foreach (var fileInfo in workDirectoryInfo.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                Interlocked.Increment(ref fileProcessCounter);
                directorySize += fileInfo.Length;
            }

            progressReportCancellationToken.Cancel();
            await progressReportTask;

            return directorySize;
        }

        private async Task FileScanReport(IExecutionContext context, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                context.Output($"Scaned {fileProcessCounter}");
                try
                {
                    await Task.Delay(1000, token);
                }
                catch (OperationCanceledException ex)
                {
                    Trace.Error(ex);
                }
            }
        }

        private int fileProcessCounter = 0;
    }
}