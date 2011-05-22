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
using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.Build.Construction;
using NAnt.Core;
using NAnt.DotNet.Tasks;
using NAnt.DotNet.Types;
using MB = Microsoft.Build.Evaluation;
using Microsoft.Build.Exceptions;

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
			var projectFileName = String.Format(
					"{0}{1}{2}.csproj",
					task.Sources.BaseDirectory.FullName,
					Path.DirectorySeparatorChar,
					Path.GetFileNameWithoutExtension(task.OutputFile.Name));

			ProjectRootElement project = null;
			if (!File.Exists(projectFileName))
				project = ProjectRootElement.Create(projectFileName);
			else
				project = ProjectRootElement.Open(projectFileName);
			var projectManipulator = new MB.Project(project);

			project.DefaultTargets = "Build";
			SetKnownProperties(project, task);
			GenerateReferences(project, projectManipulator, task, generator);
			GenerateCompileIncludes(project, projectManipulator, task);
			project.EnsureImportExists("$(MSBuildToolsPath)\\Microsoft.CSharp.targets");

			generator.RegisterProjectInSolution(project);
			project.Save();
		}

		public void TaskStarted(object sender, BuildEventArgs e)
		{
		}

		private void GenerateCompileIncludes(ProjectRootElement project, MB.Project projectManipulator, CscTask task)
		{
			projectManipulator.RemoveItems(projectManipulator.GetItemsIgnoringCondition("Compile"));
			projectManipulator.RemoveItems(projectManipulator.GetItemsIgnoringCondition("EmbeddedResource"));

			foreach (var include in task.Sources.FileNames)
			{
				project.AddItem(
					"Compile",
					MB.ProjectCollection.Escape(new FileInfo(include).GetPathRelativeTo(task.BaseDirectory)),
					new[]
					{
						new KeyValuePair<string, string>("SubType", "Code")
					});
			}
			foreach (var resourceList in task.ResourcesList)
			{
				foreach (var resource in resourceList.FileNames)
				{
					project.AddItem(
						"EmbeddedResource",
						MB.ProjectCollection.Escape(new FileInfo(resource).GetPathRelativeTo(task.BaseDirectory)),
						new[]
						{
							new KeyValuePair<string, string>("LogicalName", resourceList.GetManifestResourceName(resource))
						});
				}
			}
			project.EnsureItemExists("None", MB.ProjectCollection.Escape(
				new FileInfo(task.Project.BuildFileLocalName).GetPathRelativeTo(task.BaseDirectory)));
		}

		private void GenerateReferences(ProjectRootElement project, MB.Project projectManipulator, CscTask task, GenerateMsBuildTask generator)
		{			
			projectManipulator.RemoveItems(projectManipulator.GetItemsIgnoringCondition("Reference"));
			projectManipulator.RemoveItems(projectManipulator.GetItemsIgnoringCondition("ProjectReference"));

			foreach(var reference in task.References.FileNames)
			{
				var name = Path.GetFileNameWithoutExtension(reference);
				var relativeReference = new FileInfo(reference).GetPathRelativeTo(task.BaseDirectory);
				var matchedProject = generator.FindProjectReference(relativeReference);
				if (matchedProject == null)
				{
					project.AddItem(
						"Reference",
						name,
						new[]
						{
							new KeyValuePair<string, string>("Name", name),
							new KeyValuePair<string, string>("HintPath", MB.ProjectCollection.Escape(relativeReference))
						});
				}
				else
				{
					project.AddItem(
						"ProjectReference",
						MB.ProjectCollection.Escape(matchedProject.FullPath),
						new[]
						{
							new KeyValuePair<string, string>("Project", matchedProject.GetProjectId().ToString("B")),
							new KeyValuePair<string, string>("Package", matchedProject.GetTypeId().ToString("B"))
						});
				}
			}
			foreach (var reference in new[] { "mscorlib", "System", "System.Xml" })
			{
				project.AddItem("Reference", reference, new[] { new KeyValuePair<string, string>("Name", reference) });
			}
		}

		private void SetKnownProperties(ProjectRootElement project, CscTask task)
		{
			// MSBuild properties http://msdn.microsoft.com/en-us/library/bb629394.aspx
			// NAnt CscTask properties http://nant.sourceforge.net/nightly/latest/help/tasks/csc.html
			project.SetDefaultPropertyValue("AssemblyName", Path.GetFileNameWithoutExtension(task.OutputFile.Name));
			project.EnsurePropertyExists("ProjectGuid", Guid.NewGuid().ToString("B"));
			if (!String.IsNullOrWhiteSpace(task.BaseAddress))
				project.SetDefaultPropertyValue("BaseAddress", task.BaseAddress);
			project.SetDefaultPropertyValue("CheckForOverflowUnderflow", task.Checked.ToString());
			project.SetDefaultPropertyValue("CodePage", task.Codepage ?? String.Empty);
			project.SetDefaultPropertyValue("DebugSymbols", task.Debug.ToString());
			if (task.DebugOutput == DebugOutput.Enable)
			{
				task.DebugOutput = DebugOutput.Full;
				task.Define = String.Format("DEBUG,TRACE,{0}", task.Define);
			}
			project.SetDefaultPropertyValue("DebugType", task.DebugOutput.ToString());
			if (task.DocFile != null)
				project.SetDefaultPropertyValue("DocumentationFile", MB.ProjectCollection.Escape(task.DocFile.GetPathRelativeTo(task.BaseDirectory)));
			if (task.FileAlign > 0)
				project.SetDefaultPropertyValue("FileAlignment", task.FileAlign.ToString(CultureInfo.InvariantCulture));
			// TODO: langversion
			// TODO: noconfig
			// TODO: nostdlib
			project.SetDefaultPropertyValue("Optimize", task.Optimize.ToString());
			project.SetDefaultPropertyValue("Platform", task.Platform ?? "AnyCPU");
			project.SetDefaultPropertyValue("ProjectTypeGuids", projectTypeGuid.ToString("B"));
			project.SetDefaultPropertyValue("AllowUnsafeBlocks", task.Unsafe.ToString());
			project.SetDefaultPropertyValue("WarningLevel", task.WarningLevel ?? "4");
			project.SetDefaultPropertyValue("OutputPath", MB.ProjectCollection.Escape(task.OutputFile.Directory.GetPathRelativeTo(task.BaseDirectory)));
			project.SetDefaultPropertyValue("OutputType", task.OutputTarget);
			project.SetDefaultPropertyValue("DefineConstants", task.Define ?? String.Empty);
			// TODO: delaysign
			// TODO: keycontainer
			// TODO: main
			project.SetDefaultPropertyValue("TreatWarningsAsErrors", task.WarnAsError.ToString());
			// TODO: win32icon
			// TODO: win32res
			// TODO: implement the rest of warning-disabling/enabling logic (see CompilerBase.WriteNoWarnList())
			var warnings = new StringBuilder();
			foreach (var warning in task.SuppressWarnings)
				if (warning.IfDefined && !warning.UnlessDefined)
					warnings.AppendFormat("{0},", warning.Number);
			project.SetDefaultPropertyValue("NoWarn", warnings.ToString());
		}

		private static readonly Guid projectTypeGuid = Guid.Parse("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}");
	}
}
