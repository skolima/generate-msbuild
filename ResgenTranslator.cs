using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NAnt.Core;
using NAnt.DotNet.Tasks;

namespace GenerateMsBuildTask
{
    class ResgenTranslator : IBuildListener
    {
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
            // resources embedded in csc tasks are already handled
            if (typeof(CscTask) != e.Task.Parent.GetType())
            {                
            }
        }

        public void TaskStarted(object sender, BuildEventArgs e)
        {
        }
    }
}
