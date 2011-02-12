/*
 * generate-msbuild NAnt task
 * Copyright (C) 2011, Leszek 'skolima' Ciesielski
 * 
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program. If not, see <http://www.gnu.org/licenses/>.
 */

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
using NAnt.Core.Util;

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
            var generator = (GenerateMsBuildTask)sender;
            var project = ProjectRootElement.Create();
            var projectFileName = String.Format(
                    "{0}{1}_{2}.csproj",
                    task.Sources.BaseDirectory.FullName,
                    Path.DirectorySeparatorChar,
                    Path.GetFileNameWithoutExtension(task.OutputFile.Name));
            var projectGuid = Guid.NewGuid();

            project.DefaultTargets = "Build";
            SetKnownProperties(project.AddPropertyGroup(), task, projectGuid);
            GenerateReferences(project.AddItemGroup(), task, generator);
            GenerateCompileIncludes(project.AddItemGroup(), task);
            project.AddImport(String.Format("$(MSBuildToolsPath){0}Microsoft.CSharp.targets", Path.DirectorySeparatorChar));

            generator.RegisterProjectInSolution(task.OutputFile.FullName, projectFileName, projectTypeGuid, projectGuid);
            project.Save(projectFileName);
        }

        public void TaskStarted(object sender, BuildEventArgs e)
        {
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
                    itemGroup.AddItem(
                        "EmbeddedResource",
                        MB.ProjectCollection.Escape(resource),
                        new[]
                        {
                            new KeyValuePair<string, string>("LogicalName", resourceList.GetManifestResourceName(resource))
                        });
                }
            }
            itemGroup.AddItem("None", MB.ProjectCollection.Escape(task.Project.BuildFileLocalName));
        }

        private void GenerateReferences(ProjectItemGroupElement itemGroup, CscTask task, GenerateMsBuildTask generator)
        {
            foreach(var reference in task.References.FileNames)
            {
                var name = Path.GetFileNameWithoutExtension(reference);
                var matchedProject = generator.FindProjectReference(reference);
                if (matchedProject == null)
                {
                    itemGroup.AddItem(
                        "Reference",
                        name,
                        new[]
                        {
                            new KeyValuePair<string, string>("Name", name),
                            new KeyValuePair<string, string>("HintPath", MB.ProjectCollection.Escape(reference))
                        });
                }
                else
                {
                    itemGroup.AddItem(
                        "ProjectReference",
                        MB.ProjectCollection.Escape(matchedProject.FilePath),
                        new[]
                        {
                            new KeyValuePair<string, string>("Project", matchedProject.ProjectId.ToString("B")),
                            new KeyValuePair<string, string>("Package", matchedProject.TypeId.ToString("B"))
                        });
                }
            }
            foreach (var reference in new[] { "mscorlib", "System", "System.Xml" })
            {
                itemGroup.AddItem("Reference", reference);
            }
        }

        private void SetKnownProperties(ProjectPropertyGroupElement properties, CscTask task, Guid projectGuid)
        {
            // MSBuild properties http://msdn.microsoft.com/en-us/library/bb629394.aspx
            // NAnt CscTask properties http://nant.sourceforge.net/nightly/latest/help/tasks/csc.html
            properties.AddProperty("AssemblyName", Path.GetFileNameWithoutExtension(task.OutputFile.FullName));
            properties.AddProperty("ProjectGuid", projectGuid.ToString("B"));
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
            // TODO: implement the rest of warning-disabling/enabling logic (see CompilerBase.WriteNoWarnList())
            var warnings = new StringBuilder();
            foreach(var warning in task.SuppressWarnings)
                if(warning.IfDefined && ! warning.UnlessDefined)
                warnings.AppendFormat("{0},", warning.Number);
            properties.AddProperty("NoWarn", warnings.ToString());
        }

        private static readonly Guid projectTypeGuid = new Guid("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}");
    }
}
