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
