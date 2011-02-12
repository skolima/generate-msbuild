using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NAnt.Core;
using NAnt.Core.Attributes;

using MB = Microsoft.Build.Evaluation;

namespace GenerateMsBuildTask
{
    [TaskName("generate-msbuild")]
    public class GenerateMsBuildTask : Task, IBuildListener
    {
        private Dictionary<string, IBuildListener> taskTranslators = new Dictionary<string, IBuildListener>()
        {
            {"csc", new CscTranslator()},
            {"resgen", new ResgenTranslator()}
        };

        private MB.ProjectCollection solution = new MB.ProjectCollection();

        protected override void ExecuteTask()
        {
            Project.BuildStarted += BuildStarted;
            Project.BuildFinished += BuildFinished;
            Project.TargetStarted += TargetStarted;
            Project.TargetFinished += TargetFinished;
            Project.TaskStarted += TaskStarted;
            Project.TaskFinished += TaskFinished;
            Project.MessageLogged += MessageLogged;

            // this ensures we are propagated to child projects
            Project.BuildListeners.Add(this);
        }

        public void BuildFinished(object sender, BuildEventArgs e)
        {
        }

        public void BuildStarted(object sender, BuildEventArgs e)
        {
        }

        public void MessageLogged(object sender, BuildEventArgs e)
        {
        }

        public void TargetFinished(object sender, BuildEventArgs e)
        {
        }

        public void TargetStarted(object sender, BuildEventArgs e)
        {
        }

        public void TaskFinished(object sender, BuildEventArgs e)
        {
            if (e.Exception == null)
            {
                if (taskTranslators.ContainsKey(e.Task.Name))
                {
                    taskTranslators[e.Task.Name].TaskFinished(solution, e);
                }
            }
        }

        public void TaskStarted(object sender, BuildEventArgs e)
        {
        }
    }
}
