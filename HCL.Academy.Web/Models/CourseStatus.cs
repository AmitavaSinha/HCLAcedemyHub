namespace HCLAcademy.Models
{
    public class CourseStatus
    {        
        public string cStatus { get; set; }

        public bool isDualStatus { get; set; }
        public Colors bgColor { get; set; }
    }

    public enum Colors
    {
        red,
        blue,
        green,
        combstatus

    }
}