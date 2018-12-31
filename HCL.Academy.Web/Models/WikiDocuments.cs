using System.Collections.Generic;

namespace HCLAcademy.Models
{
    public class WikiDocuments
    {
        public string DocumentName { get; set; }

        public string DocumentURL { get; set; }

        public string ParentFolderURL { get; set; }

        public bool IsFolder { get; set; }
        
        public int  ID { get; set; }

        public string ParentFolder { get; set; }

        public List<WikiDocuments> WikiChild { get; set; }
    }


    public class FolderDocuments
    {
        public string DocumentName { get; set; }        

        public int ID { get; set; }

        public List<FolderDocuments> Subfolder { get; set; }
    }
}