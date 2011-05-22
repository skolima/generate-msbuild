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
using System.Linq;
using System.Text;
using System.IO;

namespace GenerateMsBuildTask
{
	static class FileInfoExtensions
	{
		public static string GetPathRelativeTo(this FileInfo targetInfo, DirectoryInfo baseDirectory)
		{
			return GetPathRelativeTo(targetInfo.FullName, baseDirectory.FullName);
		}

		private static string GetPathRelativeTo(string target, string baseDirectory)
		{
			Uri fromUri = new Uri(baseDirectory + Path.DirectorySeparatorChar);
			Uri toUri = new Uri(target);

			Uri relativeUri = fromUri.MakeRelativeUri(toUri);

			return relativeUri.ToString().Replace('/', Path.DirectorySeparatorChar);
		}

		public static string GetPathRelativeTo(this DirectoryInfo targetInfo, DirectoryInfo baseDirectory)
		{
			return GetPathRelativeTo(targetInfo.FullName, baseDirectory.FullName);
		}
	}
}
