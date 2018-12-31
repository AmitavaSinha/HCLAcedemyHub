using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HCLAcademy.Models
{
    public class UserCheckList
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string CheckList { get; set; }
        public string CheckListStatus { get; set; }

    }
}