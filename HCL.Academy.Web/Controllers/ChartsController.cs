using HCL.Academy.Web.DAL;
using HCLAcademy.Common;
using HCLAcademy.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Mvc;
using System.Web.UI.DataVisualization.Charting;

namespace HCLAcademy.Controllers
{
    public class ChartsController : Controller
    {
        /// <summary>
        /// Gets the heat map of the selected project and competency.
        /// </summary>
        /// <param name="projectId"></param>
        /// <param name="competency"></param>
        /// <returns></returns>
        public ActionResult HeatMap(int projectId, string competency)
        {
            var imgStream = new MemoryStream();
            var heatMapChart = new Chart()
            {
                Width = 600,
                Height = 300
            };
            try
            {
                IDAL dal = (new DALFactory()).GetInstance();
                Resource prjRes = dal.GetResourceDetailsByProjectID(projectId);

                List<string> xValues = new List<string>();
                List<double> yValues = new List<double>();
                List<double> yValues2 = new List<double>();

                for (int k = 0; k < prjRes.allResources.Count; k++)
                {
                    xValues.Add(prjRes.allResources[k].skill);
                    if (competency.ToUpper() == "BEGINNER" || competency.ToUpper() == "NOVICE")
                    {
                        yValues.Add(prjRes.allResources[k].expectedBeginnerCount);
                        yValues2.Add(prjRes.allResources[k].availableBeginnerCount);

                    }
                    else if (competency.ToUpper() == "ADVANCEDBEGINNER")
                    {
                        yValues.Add(prjRes.allResources[k].ExpectedadvancedBeginnerCount);
                        yValues2.Add(prjRes.allResources[k].AvailableadvancedBeginnerCount);
                    }
                    else if (competency.ToUpper() == "COMPETENT")
                    {
                        yValues.Add(prjRes.allResources[k].ExpectedcompetentCount);
                        yValues2.Add(prjRes.allResources[k].AvailablecompetentCount);
                    }
                    else if (competency.ToUpper() == "PROFICIENT")
                    {
                        yValues.Add(prjRes.allResources[k].expectedProficientCount);
                        yValues2.Add(prjRes.allResources[k].availableProficientCount);
                    }
                    else if (competency.ToUpper() == "EXPERT")
                    {
                        yValues.Add(prjRes.allResources[k].expectedExpertCount);
                        yValues2.Add(prjRes.allResources[k].availableExpertCount);
                    }
                }

                Series s1 = new Series();
                s1.Name = "Expected";
                s1.ChartType = SeriesChartType.Radar;
                s1.MarkerBorderColor = System.Drawing.Color.FromArgb(64, 64, 64);
                s1.MarkerSize = 9;
                s1.BorderColor = System.Drawing.Color.FromArgb(180, 26, 59, 105);
                s1.Color = System.Drawing.Color.FromArgb(220, 65, 140, 240);
                s1.ShadowOffset = 1;
                heatMapChart.Series.Add(s1);

                Series s2 = new Series();
                s2.Name = "Available";
                s2.ChartType = SeriesChartType.Radar;
                s2.MarkerBorderColor = System.Drawing.Color.FromArgb(64, 64, 64);
                s2.MarkerSize = 9;
                s2.BorderColor = System.Drawing.Color.FromArgb(180, 26, 59, 105);
                s2.Color = System.Drawing.Color.FromArgb(220, 252, 180, 65);
                s2.ShadowOffset = 1;
                heatMapChart.Series.Add(s2);

                heatMapChart.Series["Expected"].Points.DataBindXY(xValues, yValues);
                heatMapChart.Series["Available"].Points.DataBindXY(xValues, yValues2);
                heatMapChart.BorderSkin.SkinStyle = BorderSkinStyle.Emboss;

                Legend l = new Legend();
                l.IsTextAutoFit = false;
                l.Name = "Default";
                l.BackColor = System.Drawing.Color.Transparent;
                l.Font = new System.Drawing.Font("Trebuchet MS", 8, System.Drawing.FontStyle.Bold);
                l.Alignment = System.Drawing.StringAlignment.Far;
                l.Position.Y = 74;
                l.Position.Height = 14;
                l.Position.Width = 19;
                l.Position.X = 74;
                heatMapChart.Legends.Add(l);
                ChartArea c = new ChartArea();
                c.BorderColor = System.Drawing.Color.FromArgb(64, 64, 64, 64);
                c.BackSecondaryColor = System.Drawing.Color.White;
                c.BackColor = System.Drawing.Color.OldLace;
                c.ShadowColor = System.Drawing.Color.Transparent;
                c.Area3DStyle.Rotation = 10;
                c.Area3DStyle.Perspective = 10;
                c.Area3DStyle.Inclination = 15;
                c.Area3DStyle.IsRightAngleAxes = false;
                c.Area3DStyle.WallWidth = 0;
                c.Area3DStyle.IsClustered = false;
                c.Position.Y = 15;
                c.Position.Height = 78;
                c.Position.Width = 88;
                c.Position.X = 5;
                c.AxisY.LineColor = System.Drawing.Color.FromArgb(64, 64, 64, 64);
                c.AxisY.LabelStyle.Font = new System.Drawing.Font("Trebuchet MS", 8, System.Drawing.FontStyle.Bold);
                c.AxisY.MajorGrid.LineColor = System.Drawing.Color.FromArgb(64, 64, 64, 64);
                c.AxisX.LineColor = System.Drawing.Color.FromArgb(64, 64, 64, 64);
                c.AxisX.LabelStyle.Font = new System.Drawing.Font("Trebuchet MS", 8, System.Drawing.FontStyle.Bold);
                c.AxisX.MajorGrid.LineColor = System.Drawing.Color.FromArgb(64, 64, 64, 64);
                heatMapChart.ChartAreas.Add(c);

                // Save the chart to a MemoryStream                
                heatMapChart.SaveImage(imgStream, ChartImageFormat.Png);
                imgStream.Seek(0, SeekOrigin.Begin);
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "Charts, HeatMap", ex.Message, ex.StackTrace));

            }
            // Return the contents of the Stream to the client
            return File(imgStream, "image/png");
        }
        /// <summary>
        /// Fetches the average of the selected project
        /// </summary>
        /// <param name="projectId"></param>
        /// <returns></returns>
        public ActionResult AverageHeatMap(int projectId)
        {
            var heatMapChart = new Chart()
            {
                Width = 600,
                Height = 300
            };
            var imgStream = new MemoryStream();
            try
            {
                IDAL dal = (new DALFactory()).GetInstance();
                Resource prjRes = dal.GetResourceDetailsByProjectID(projectId);

                List<string> xValues = new List<string>();
                List<double> yValues = new List<double>();
                List<double> yValues2 = new List<double>();

                for (int k = 0; k < prjRes.allResources.Count; k++)
                {
                    xValues.Add(prjRes.allResources[k].skill);

                    double expectedTotal = prjRes.allResources[k].expectedBeginnerCount + prjRes.allResources[k].ExpectedadvancedBeginnerCount * 2 + prjRes.allResources[k].ExpectedcompetentCount * 3 + prjRes.allResources[k].expectedProficientCount * 4 + prjRes.allResources[k].expectedExpertCount * 5;
                    double expectedAverage = expectedTotal / (prjRes.allResources[k].expectedBeginnerCount + prjRes.allResources[k].ExpectedadvancedBeginnerCount + prjRes.allResources[k].ExpectedcompetentCount + prjRes.allResources[k].expectedProficientCount + prjRes.allResources[k].expectedExpertCount);
                    yValues.Add(expectedAverage);

                    double availableTotal = prjRes.allResources[k].availableBeginnerCount + prjRes.allResources[k].AvailableadvancedBeginnerCount * 2 + prjRes.allResources[k].AvailablecompetentCount * 3 + prjRes.allResources[k].availableProficientCount * 4 + prjRes.allResources[k].availableExpertCount * 5;
                    double availableAverage = availableTotal / (prjRes.allResources[k].availableBeginnerCount + prjRes.allResources[k].AvailableadvancedBeginnerCount + prjRes.allResources[k].AvailablecompetentCount + prjRes.allResources[k].availableProficientCount + prjRes.allResources[k].availableExpertCount);

                    yValues2.Add(availableAverage);
                }

                Series s1 = new Series();
                s1.Name = "Expected";
                s1.ChartType = SeriesChartType.Radar;
                s1.MarkerBorderColor = System.Drawing.Color.FromArgb(64, 64, 64);
                s1.MarkerSize = 9;
                s1.BorderColor = System.Drawing.Color.FromArgb(180, 26, 59, 105);
                s1.Color = System.Drawing.Color.FromArgb(220, 65, 140, 240);
                s1.ShadowOffset = 1;
                heatMapChart.Series.Add(s1);

                Series s2 = new Series();
                s2.Name = "Available";
                s2.ChartType = SeriesChartType.Radar;
                s2.MarkerBorderColor = System.Drawing.Color.FromArgb(64, 64, 64);
                s2.MarkerSize = 9;
                s2.BorderColor = System.Drawing.Color.FromArgb(180, 26, 59, 105);
                s2.Color = System.Drawing.Color.FromArgb(220, 252, 180, 65);
                s2.ShadowOffset = 1;
                heatMapChart.Series.Add(s2);

                heatMapChart.Series["Expected"].Points.DataBindXY(xValues, yValues);
                heatMapChart.Series["Available"].Points.DataBindXY(xValues, yValues2);
                heatMapChart.BorderSkin.SkinStyle = BorderSkinStyle.Emboss;

                Legend l = new Legend();
                l.IsTextAutoFit = false;
                l.Name = "Default";
                l.BackColor = System.Drawing.Color.Transparent;
                l.Font = new System.Drawing.Font("Trebuchet MS", 8, System.Drawing.FontStyle.Bold);
                l.Alignment = System.Drawing.StringAlignment.Far;
                l.Position.Y = 74;
                l.Position.Height = 14;
                l.Position.Width = 19;
                l.Position.X = 74;
                heatMapChart.Legends.Add(l);
                ChartArea c = new ChartArea();
                c.BorderColor = System.Drawing.Color.FromArgb(64, 64, 64, 64);
                c.BackSecondaryColor = System.Drawing.Color.White;
                c.BackColor = System.Drawing.Color.OldLace;
                c.ShadowColor = System.Drawing.Color.Transparent;
                c.Area3DStyle.Rotation = 10;
                c.Area3DStyle.Perspective = 10;
                c.Area3DStyle.Inclination = 15;
                c.Area3DStyle.IsRightAngleAxes = false;
                c.Area3DStyle.WallWidth = 0;
                c.Area3DStyle.IsClustered = false;
                c.Position.Y = 15;
                c.Position.Height = 78;
                c.Position.Width = 88;
                c.Position.X = 5;
                c.AxisY.LineColor = System.Drawing.Color.FromArgb(64, 64, 64, 64);
                c.AxisY.LabelStyle.Font = new System.Drawing.Font("Trebuchet MS", 8, System.Drawing.FontStyle.Bold);
                c.AxisY.MajorGrid.LineColor = System.Drawing.Color.FromArgb(64, 64, 64, 64);
                c.AxisX.LineColor = System.Drawing.Color.FromArgb(64, 64, 64, 64);
                c.AxisX.LabelStyle.Font = new System.Drawing.Font("Trebuchet MS", 8, System.Drawing.FontStyle.Bold);
                c.AxisX.MajorGrid.LineColor = System.Drawing.Color.FromArgb(64, 64, 64, 64);
                heatMapChart.ChartAreas.Add(c);

                // Save the chart to a MemoryStream

                heatMapChart.SaveImage(imgStream, ChartImageFormat.Png);
                imgStream.Seek(0, SeekOrigin.Begin);
            }
            catch (Exception ex)
            {
                UserManager users = (UserManager)Session["CurrentUser"];
                LogHelper.AddLog(new LogEntity(AppConstant.PartitionError, users.EmailID.ToString(), AppConstant.ApplicationName, "Charts, AverageHeatMap", ex.Message, ex.StackTrace));
            }
            // Return the contents of the Stream to the client
            return File(imgStream, "image/png");
        }
    }
}
