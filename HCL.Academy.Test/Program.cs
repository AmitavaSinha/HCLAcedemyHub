using HCL.Academy.Report;
using System.Collections.Generic;

namespace HCL.Academy.Test
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            List<HCL.Academy.Report.Models.TrainingReport> trainingReports = new List<HCL.Academy.Report.Models.TrainingReport>();

            HCL.Academy.Report.Models.TrainingReport report1 = new HCL.Academy.Report.Models.TrainingReport()
            {
                Skill="Java",
                Level="Expert",
                User="Prasenjit",
                Status="Draft"
            };
            trainingReports.Add(report1);
            HCL.Academy.Report.Models.TrainingReport report2 = new HCL.Academy.Report.Models.TrainingReport()
            {
                Skill = "Java",
                Level = "Novice",
                User = "Janajit",
                Status = "Haha"
            };
            trainingReports.Add(report2);


            HCL.Academy.Report.TrainingReport report = new HCL.Academy.Report.TrainingReport(trainingReports);
            report.Generate(@"C:\Temp\Jitendra.xlsx");
        }
    }
}
