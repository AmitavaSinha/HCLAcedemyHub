using System.Collections.Generic;

namespace HCL.Academy.Web.Models
{
   
    public class CheckListItem
    {
        public int ID { get; set; }
        public string Name { get; set; }
        
        public string InternalName { get; set; }
        
        public string Desc { get; set; }
        
        public string Choice { get; set; }
        public string GEOName { get; set; }
        public int GEOId { get; set; }

        public string RoleName { get; set; }
        public int RoleId { get; set; }
    }
}