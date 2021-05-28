using EnvDTE;

namespace COFRS.Template.Common.Models
{
    public class ProjectItemInfo
    {
        public ProjectItem ProjectItem { get; set; }
        public bool ContainsComposits { get; set; }
        public bool ContainsTables { get; set; }
        public bool ContainsEnums { get; set; }
        public bool ContainsResources { get; set; }
    }
}
