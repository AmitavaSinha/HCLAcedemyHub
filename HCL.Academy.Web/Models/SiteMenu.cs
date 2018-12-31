using System.Collections.Generic;

namespace HCLAcademy.Models
{
    public class SiteMenu
    {
        public int ItemId { get; set; }

        public string ItemName { get; set; }

        public int ParentItemId { get; set; }

        public int ItemOrdering { get; set; }

        public string ItemURL { get; set; }

        public string ItemTarget { get; set; }

        public string ItemHidden { get; set; }
        public List<UserRole> UserRole { get; set; }
    }

    public class UserRole
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; }
        public int UserId { get; set; }
    }
}