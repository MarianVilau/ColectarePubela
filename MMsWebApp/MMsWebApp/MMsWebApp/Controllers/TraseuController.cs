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
using MMsWebApp.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

namespace MMsWebApp.Controllers
{
    public class TraseuController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public TraseuController(IHttpClientFactory httpClientFactory, AppDbContext context, IWebHostEnvironment environment)
        {
            _httpClientFactory = httpClientFactory;
            _context = context;
            _environment = environment;
        }

        public async Task<IActionResult> Index(DateTime? selectedDate = null)
        {
            // If no date is selected, use today's date
            var date = selectedDate ?? DateTime.Today;

            // Get all unique dates from the database for the dropdown
            var availableDates = await _context.Colectari
                .Select(c => c.CollectedAt.Date)
                .Distinct()
                .OrderByDescending(d => d)
                .ToListAsync();

            // Get collections for the selected date
            var collections = await _context.Colectari
                .Include(c => c.Pubela)
                .Where(c => c.CollectedAt.Date == date.Date)
                .OrderBy(c => c.CollectedAt)
                .ToListAsync();

            // Calculate statistics
            var points = collections.Select(c => new TraseuPoint
            {
                NrMasina = "SB 77 ULB", // Hardcoded for now
                IdPubela = c.IdPubela,
                ColectatLa = c.CollectedAt,
                Adresa = c.Adresa,
                Latitude = c.Latitude,
                Longitude = c.Longitude
            }).ToList();

            var optimizedRoute = await OptimizeRoute(points);
            var originalDistance = CalculateTotalDistance(points);
            var optimizedDistance = CalculateTotalDistance(optimizedRoute);
            var distanceDifference = originalDistance - optimizedDistance;
            var optimizedTime = optimizedRoute.Count * 1; // 1 minute per collection

            ViewBag.AvailableDates = availableDates;
            ViewBag.SelectedDate = date;
            ViewBag.TotalLines = points.Count;
            ViewBag.PointCount = points.Count;
            ViewBag.FirstPoint = points.FirstOrDefault();
            ViewBag.LastPoint = points.LastOrDefault();
            ViewBag.DistanceDifference = distanceDifference;
            ViewBag.OptimizedTime = optimizedTime;

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

        [HttpPost]
        public async Task<IActionResult> Import()
        {
            try
            {
                var filePath = Path.Combine(_environment.ContentRootPath, "Data", "Traseu2.csv");
                
                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true,
                    Delimiter = ",",
                    MissingFieldFound = null
                };

                using (var reader = new StreamReader(filePath))
                using (var csv = new CsvReader(reader, config))
                {
                    var records = csv.GetRecords<TraseuPoint>().ToList();
                    
                    // Exclude first and last entry
                    records = records.Skip(1).Take(records.Count - 2).ToList();

                    // Group records by IdPubela to avoid duplicates
                    var groupedPubele = records
                        .Where(r => !string.IsNullOrEmpty(r.IdPubela))
                        .GroupBy(r => r.IdPubela)
                        .ToList();

                    // First, create or update all Pubele
                    foreach (var group in groupedPubele)
                    {
                        var pubelaId = group.Key;
                        var existingPubela = await _context.Pubele
                            .FirstOrDefaultAsync(p => p.Id == pubelaId);

                        if (existingPubela == null)
                        {
                            existingPubela = new Pubela
                            {
                                Id = pubelaId,
                                Tip = "Standard" // Set default type
                            };
                            _context.Pubele.Add(existingPubela);
                            await _context.SaveChangesAsync(); // Save to get the ID
                        }
                    }

                    // Then create all Colectari with proper relationships
                    foreach (var record in records.Where(r => !string.IsNullOrEmpty(r.IdPubela)))
                    {
                        var pubela = await _context.Pubele
                            .Include(p => p.Colectari)
                            .FirstOrDefaultAsync(p => p.Id == record.IdPubela);

                        if (pubela != null)
                        {
                            var colectare = new Colectare
                            {
                                IdPubela = record.IdPubela,
                                CollectedAt = record.ColectatLa,
                                Adresa = record.Adresa,
                                Latitude = record.Latitude,
                                Longitude = record.Longitude,
                                Pubela = pubela // Set the navigation property
                            };

                            _context.Colectari.Add(colectare);
                            pubela.Colectari.Add(colectare); // Add to the navigation collection
                        }
                    }

                    await _context.SaveChangesAsync();
                }

                TempData["Message"] = "Import realizat cu succes! Relațiile între tabele au fost create corect.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Eroare la import: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
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