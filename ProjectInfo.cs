using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GenerateMsBuildTask
{
    class ProjectInfo
    {
        public ProjectInfo(Guid projectTypeGuid, string projectFilePath, Guid projectGuid)
        {
            TypeId = projectTypeGuid;
            FilePath = projectFilePath;
            ProjectId = projectGuid;
        }
        public string FilePath { get; private set; }
        public Guid TypeId { get; private set; }
        public Guid ProjectId { get; private set; }
    }
}
