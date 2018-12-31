using System.Collections.Generic;

namespace HCLAcademy.Models
{
    public class TrainingPlan
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string ContentBody { get; set; }
        public string ContentUrl { get; set; }
        public int OrderBy { get; set; }
        public string Name { get; set; }
        public string ParentFolderURL { get; set; }
        public bool IsFolder { get; set; }
        public string ParentFolder { get; set; }
        public string DocumentName { get; set; }
        public string DocumentURL { get; set; }
        
        public List<TrainingPlan> TrainingChild { get; set; }
    }

    public class ContentFolder
    {
        public string Name { get; set; }

        public int OrderBy { get; set; }

        public List<ContentFolder> Subfolder { get; set; }
    }

   

}