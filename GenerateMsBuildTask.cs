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
using System.Collections.Specialized;
using System.Linq;
using System.Text;

using NAnt.Core;
using NAnt.Core.Attributes;

using MB = Microsoft.Build.Evaluation;
using System.IO;
using Microsoft.Build.Construction;

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

        private IDictionary<string, ProjectRootElement> projectOutputs = new Dictionary<string, ProjectRootElement>();

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
            if (e.Exception == null && e.Project == Project)
            {
                GenerateSolutionFile(Project);
            }
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
            if (e.Exception == null && !string.IsNullOrEmpty(e.Task.Name))
            {
                if (taskTranslators.ContainsKey(e.Task.Name))
                {
                    taskTranslators[e.Task.Name].TaskFinished(this, e);
                }
            }
        }

        public void TaskStarted(object sender, BuildEventArgs e)
        {
        }

        internal ProjectRootElement FindProjectReference(string dependencyFileName)
        {
            return projectOutputs.ContainsKey(dependencyFileName)
                ? projectOutputs[dependencyFileName]
                : null;
        }

        internal void RegisterProjectInSolution(ProjectRootElement projectRoot)
        {
            projectOutputs[projectRoot.GetOutputFileName()] = projectRoot;
        }

        private void GenerateSolutionFile(NAnt.Core.Project Project)
        {
            var solutionPath = String.Format(
                    "{0}{1}_{2}.sln",
                    new FileInfo(Project.BuildFileLocalName).DirectoryName,
                    Path.DirectorySeparatorChar,
                    Project.ProjectName);
            using (var solution = File.CreateText(solutionPath))
            {
                solution.WriteLine(@"Microsoft Visual Studio Solution File, Format Version 11.00");
                solution.WriteLine("# Visual Studio 2010");
                foreach (var project in projectOutputs.Values)
                {
                    solution.WriteLine(
                        "Project(\"{0:B}\") = \"{1}\", \"{1}\", \"{2:B}\"",
                        project.GetTypeId(),
                        project.FullPath,
                        project.GetProjectId());
                    solution.WriteLine("EndProject");
                }
                solution.WriteLine("Project(\"{2150E333-8FDC-42A3-9474-1A3956D46DE8}\") = \"Solution Items\", \"Solution Items\", \"{4D8FAB75-E6D2-4581-B7F0-BB11BCCEE0CA}\"");
                solution.WriteLine("	ProjectSection(SolutionItems) = preProject");
                solution.WriteLine("		{0} = {0}", Project.BuildFileLocalName);
                solution.WriteLine("	EndProjectSection");
                solution.WriteLine("EndProject");
            }
        }
    }
}
