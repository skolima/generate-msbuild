using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NAnt.Core;
using NAnt.DotNet.Tasks;
using MB = Microsoft.Build.Evaluation;
using Microsoft.Build.Construction;
using System.Globalization;
using NAnt.DotNet.Types;

namespace GenerateMsBuildTask
{
    class CscTranslator : IBuildListener
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
            var task = (CscTask)e.Task;
            var solution = (MB.ProjectCollection)sender;
            var project = ProjectRootElement.Create(solution);
            project.DefaultTargets = "Build";
            SetKnownProperties(project.AddPropertyGroup(), task);
            GenerateReferences(project.AddItemGroup(), task);
            GenerateCompileIncludes(project.AddItemGroup(), task);
            project.AddImport(String.Format("$(MSBuildToolsPath){0}Microsoft.CSharp.targets", Path.DirectorySeparatorChar));

            project.Save(String.Format(
                "{0}{1}_{2}.csproj",
                task.Sources.BaseDirectory.FullName,
                Path.DirectorySeparatorChar,
                Path.GetFileNameWithoutExtension(task.OutputFile.Name)));
        }

        private void GenerateCompileIncludes(ProjectItemGroupElement itemGroup, CscTask task)
        {
            foreach (var include in task.Sources.FileNames)
            {
                itemGroup.AddItem(
                    "Compile",
                    MB.ProjectCollection.Escape(include),
                    new[]
                    {
                        new KeyValuePair<string, string>("SubType", "Code")
                    });
            }
            foreach (var resourceList in task.ResourcesList)
            {
                foreach (var resource in resourceList.FileNames)
                {
                    itemGroup.AddItem("EmbeddedResource", MB.ProjectCollection.Escape(resource));
                }
            }
            itemGroup.AddItem("None", MB.ProjectCollection.Escape(task.Project.BuildFileLocalName));
        }

        private void GenerateReferences(ProjectItemGroupElement itemGroup, CscTask task)
        {
            foreach(var reference in task.References.FileNames)
            {
                var name = Path.GetFileNameWithoutExtension(reference);
                itemGroup.AddItem(
                    "Reference",
                    name,
                    new[]
                    {
                        new KeyValuePair<string, string>("Name", name),
                        new KeyValuePair<string, string>("HintPath", MB.ProjectCollection.Escape(reference))
                    });            
            }
            foreach (var reference in new[] { "mscorlib", "System", "System.Xml" })
            {
                itemGroup.AddItem("Reference", reference);
            }
        }

        private void SetKnownProperties(ProjectPropertyGroupElement properties, CscTask task)
        {
            // MSBuild properties http://msdn.microsoft.com/en-us/library/bb629394.aspx
            // NAnt CscTask properties http://nant.sourceforge.net/nightly/latest/help/tasks/csc.html
            if(!String.IsNullOrWhiteSpace(task.BaseAddress))
                properties.AddProperty("BaseAddress", task.BaseAddress);
            properties.AddProperty("CheckForOverflowUnderflow", task.Checked.ToString());
            properties.AddProperty("CodePage", task.Codepage ?? String.Empty);
            properties.AddProperty("DebugSymbols", task.Debug.ToString());
            if (task.DebugOutput == DebugOutput.Enable)
            {
                task.DebugOutput = DebugOutput.Full;
                task.Define = String.Format("DEBUG,TRACE,{0}", task.Define);
            }
            properties.AddProperty("DebugType", task.DebugOutput.ToString());
            if(task.DocFile != null)
                properties.AddProperty("DocumentationFile", MB.ProjectCollection.Escape(task.DocFile.FullName));
            if(task.FileAlign > 0)
                properties.AddProperty("FileAlignment", task.FileAlign.ToString(CultureInfo.InvariantCulture));
            // TODO: langversion
            // TODO: noconfig
            // TODO: nostdlib
            properties.AddProperty("Optimize", task.Optimize.ToString());
            properties.AddProperty("Platform", task.Platform ?? "AnyCPU");
            properties.AddProperty("AllowUnsafeBlocks", task.Unsafe.ToString());
            properties.AddProperty("WarningLevel", task.WarningLevel ?? "4");
            properties.AddProperty("OutputPath", MB.ProjectCollection.Escape(task.OutputFile.Directory.FullName));
            properties.AddProperty("OutputType", task.OutputTarget);
            properties.AddProperty("DefineConstants", task.Define ?? String.Empty);
            // TODO: delaysign
            // TODO: keycontainer
            // TODO: main
            properties.AddProperty("TreatWarningsAsErrors", task.WarnAsError.ToString());
            // TODO: win32icon
            // TODO: win32res
            // TODO: warnings to ignore (see CompilerBase.WriteNoWarnList())
        }

        public void TaskStarted(object sender, BuildEventArgs e)
        {
        }
    }
}
