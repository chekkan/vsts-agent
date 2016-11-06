using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Maintain
{
    public sealed class MaintainJobExtension : AgentService, IJobExtension
    {
        public Type ExtensionType => typeof(IJobExtension);
        public string HostType => "maintain";
        public IStep PrepareStep { get; private set; }
        public IStep FinallyStep { get; private set; }

        public MaintainJobExtension()
        {
            // PrepareStep = new JobExtensionRunner(
            //     runAsync: PrepareAsync,
            //     alwaysRun: false,
            //     continueOnError: false,
            //     critical: true,
            //     displayName: StringUtil.Loc("GetSources"),
            //     enabled: true,
            //     @finally: false);

            // FinallyStep = new JobExtensionRunner(
            //     runAsync: FinallyAsync,
            //     alwaysRun: false,
            //     continueOnError: false,
            //     critical: false,
            //     displayName: StringUtil.Loc("Cleanup"),
            //     enabled: true,
            //     @finally: true);
        }

        public string GetRootedPath(IExecutionContext context, string path)
        {
            throw new NotImplementedException();
        }

        public void ConvertLocalPath(IExecutionContext context, string localPath, out string repoName, out string sourcePath)
        {
            throw new NotImplementedException();
        }
    }
}