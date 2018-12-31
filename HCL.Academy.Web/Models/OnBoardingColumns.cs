using System.Collections.Generic;
using System.Xml.Serialization;


namespace HCLAcademy.Models
{
    [XmlRoot("OnBoarding")]
    public class OnBoardingColumns
    {
        [XmlElement("Column")]
        public List<Column> OnBoarding { get; set; }
    }
    public class Column
    {
        [XmlElement("Name")]
        public string Name { get; set; }
        [XmlElement("InternalName")]
        public string InternalName { get; set; }
        [XmlElement("Desc")]
        public string Desc { get; set; }
        [XmlElement("TypeChoice")]
        public string Choice { get; set; }
    }
}