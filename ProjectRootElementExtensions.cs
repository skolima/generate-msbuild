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
using System.IO;
using System.Linq;
using Microsoft.Build.Construction;

namespace GenerateMsBuildTask
{
	static class ProjectRootElementExtensions
	{
		public static Guid GetTypeId(this ProjectRootElement project)
		{
			var value = project.Properties.Where(p => p.Name == "ProjectTypeGuids").First().Value;
			var split = value.Split(new[] {';'}, 1, StringSplitOptions.RemoveEmptyEntries);
			return Guid.Parse(split[0]);
		}

		public static Guid GetProjectId(this ProjectRootElement project)
		{
			var value = project.Properties.Where(p => p.Name == "ProjectGuid").First().Value;
			return Guid.Parse(value);
		}

		public static string GetOutputFileName(this ProjectRootElement project)
		{
			string extension;
			switch(project.GetOutputType().ToLower())
			{
				case "exe": extension = ".exe"; break;
				case "library": extension = ".dll"; break;
				case "winexe": extension = ".exe"; break;
				default: extension = string.Empty; break;
			}

			return string.Format("{0}{3}{1}{2}", project.GetOutputPath(), project.GetAssemblyName(), extension, Path.DirectorySeparatorChar);
		}

		public static string GetOutputPath(this ProjectRootElement project)
		{
			return project.Properties.Where(p => p.Name == "OutputPath").First().Value;
		}

		public static string GetAssemblyName(this ProjectRootElement project)
		{
			return project.Properties.Where(p => p.Name == "AssemblyName").First().Value;
		}

		public static string GetOutputType(this ProjectRootElement project)
		{
			return project.Properties.Where(p => p.Name == "OutputType").First().Value;
		}

		public static void EnsureImportExists(this ProjectRootElement project, string importedProject)
		{
			var existing = project.Imports.Where(p => p.Project.Equals(importedProject, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
			if (existing == null)
				project.AddImport(importedProject);
		}

		public static void EnsurePropertyExists(this ProjectRootElement project, string name, string value)
		{
			var existing = project.Properties.Where(
				p => p.Name == name && string.IsNullOrEmpty(p.Condition) && string.IsNullOrEmpty(p.Parent.Condition))
				.FirstOrDefault();
			if (existing == null)
				project.AddProperty(name, value);
		}

		public static void SetDefaultPropertyValue(this ProjectRootElement project, string name, string value)
		{
			var existing = project.Properties.Where(
				p => p.Name == name && string.IsNullOrEmpty(p.Condition) && string.IsNullOrEmpty(p.Parent.Condition))
				.FirstOrDefault();
			if (existing == null)
				existing = project.AddProperty(name, value);
			else
				existing.Value = value;
		}

		public static void EnsureItemExists(this ProjectRootElement project, string itemType, string include)
		{
			var existing = project.Items.Where(p => p.ItemType == itemType && p.Include == include).FirstOrDefault();
			if (existing == null)
				project.AddItem(itemType, include);
		}
	}
}
