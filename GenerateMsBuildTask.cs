using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NAnt.Core;
using NAnt.Core.Attributes;

namespace GenerateMsBuildTask
{
    [TaskName("generate-msbuild")]
    public class GenerateMsBuildTask : Task
    {
        protected override void ExecuteTask()
        {
            Project.BuildFinished += BuildFinished;
            Project.TaskFinished += TaskFinished;
        }

        protected void BuildFinished(object sender, BuildEventArgs args)
        {

        }

        protected void TaskFinished(object sender, BuildEventArgs args)
        {
            if (args.Exception == null)
            {
                switch(args.Task.Name)
                {
                    case "csc": break;
                    case "resgen": break;
                    case "mkdir": break;
                    case "delete": break;
                    case "nant": break; // TODO: this has to be handled in TaskStarted to properly handle chaining
                }
            }
        }
    }
}
