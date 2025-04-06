using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MMsWebApp.Models;
using CsvHelper;
using CsvHelper.Configuration;
using System.Collections.Generic;
using System.Formats.Asn1;

namespace MMsWebApp.Controllers
{
    public class TraseuController : Controller
    {
        public async Task<IActionResult> Index()
        {
            var points = await GetPointsFromCsv("Data/Traseu2.csv");
            var optimizedRoute = OptimizeRoute(points);

            ViewBag.TotalLines = points.Count;
            ViewBag.PointCount = points.Count;
            ViewBag.FirstPoint = points.FirstOrDefault();
            ViewBag.LastPoint = points.LastOrDefault();

            return View(points);
        }

        private async Task<List<TraseuPoint>> GetPointsFromCsv(string filePath)
        {
            var points = new List<TraseuPoint>();

            using (var reader = new StreamReader(filePath))
            using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture) { Delimiter = "," }))
            {
                await foreach (var record in csv.GetRecordsAsync<TraseuPoint>())
                {
                    points.Add(record);
                }
            }

            return points;
        }

        private List<TraseuPoint> OptimizeRoute(List<TraseuPoint> points)
        {
            // Implement TSP algorithm here
            // For simplicity, we will return the points as is
            return points.OrderBy(p => p.ColectatLa).ToList();
        }
    }
}