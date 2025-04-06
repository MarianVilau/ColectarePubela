using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MMsWebApp.Models;
using CsvHelper;
using CsvHelper.Configuration;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;

namespace MMsWebApp.Controllers
{
    public class TraseuController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public TraseuController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> Index()
        {
            var points = await GetPointsFromCsv("Data/Traseu2.csv");
            var filteredPoints = points.Where(p => p.ColectatLa.Date == new DateTime(2024, 10, 15)).ToList();
            var optimizedRoute = await OptimizeRoute(filteredPoints);

            var originalDistance = CalculateTotalDistance(filteredPoints);
            var optimizedDistance = CalculateTotalDistance(optimizedRoute);
            var distanceDifference = originalDistance - optimizedDistance;
            var optimizedTime = optimizedRoute.Count * 1; // 1 minute per collection

            ViewBag.TotalLines = filteredPoints.Count;
            ViewBag.PointCount = filteredPoints.Count;
            ViewBag.FirstPoint = filteredPoints.FirstOrDefault();
            ViewBag.LastPoint = filteredPoints.LastOrDefault();
            ViewBag.DistanceDifference = distanceDifference;
            ViewBag.OptimizedTime = optimizedTime;

            return View(filteredPoints);
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




        private async Task<List<TraseuPoint>> OptimizeRoute(List<TraseuPoint> points)
        {
            // Implement logic to call Mapbox Matrix API and solve TSP
            // Save the API responses to avoid exceeding the request limit

            // For simplicity, we will return the points as is
            return points.OrderBy(p => p.ColectatLa).ToList();
        }

        private double CalculateTotalDistance(List<TraseuPoint> points)
        {
            double totalDistance = 0.0;
            for (int i = 0; i < points.Count - 1; i++)
            {
                totalDistance += CalculateDistance(points[i], points[i + 1]);
            }
            return totalDistance;
        }

        private double CalculateDistance(TraseuPoint point1, TraseuPoint point2)
        {
            var R = 6371e3; // metres
            var φ1 = point1.Latitude * Math.PI / 180;
            var φ2 = point2.Latitude * Math.PI / 180;
            var Δφ = (point2.Latitude - point1.Latitude) * Math.PI / 180;
            var Δλ = (point2.Longitude - point1.Longitude) * Math.PI / 180;

            var a = Math.Sin(Δφ / 2) * Math.Sin(Δφ / 2) +
                    Math.Cos(φ1) * Math.Cos(φ2) *
                    Math.Sin(Δλ / 2) * Math.Sin(Δλ / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            var distance = R * c; // in metres
            return distance / 1000; // convert to kilometers
        }

        private async Task<TraseuPoint> GetCoordinates(string address)
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync($"https://maps.googleapis.com/maps/api/geocode/json?address={address}&key=YOUR_API_KEY");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var geocodeResponse = JsonSerializer.Deserialize<GeocodeResponse>(content);

            if (geocodeResponse?.Results?.FirstOrDefault() != null)
            {
                var location = geocodeResponse.Results.First().Geometry.Location;
                return new TraseuPoint
                {
                    NrMasina = "Unknown",
                    IdPubela = "Unknown",
                    Adresa = address,
                    Latitude = location.Lat,
                    Longitude = location.Lng
                };
            }

            return new TraseuPoint
            {
                NrMasina = "Unknown",
                IdPubela = "Unknown",
                Adresa = address
            };
        }
    }

    public class GeocodeResponse
    {
        public List<GeocodeResult> Results { get; set; }
    }

    public class GeocodeResult
    {
        public Geometry Geometry { get; set; }
    }

    public class Geometry
    {
        public Location Location { get; set; }
    }

    public class Location
    {
        public double Lat { get; set; }
        public double Lng { get; set; }
    }
}