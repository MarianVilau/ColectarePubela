using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using MMsWebApp.Models;
using CsvHelper;
using CsvHelper.Configuration;
using System.Text.Json;
using MMsWebApp.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using MMsWebApp.Hubs;

namespace MMsWebApp.Controllers
{
    public class TraseuController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IHubContext<RouteHub> _hubContext;

        public TraseuController(IHttpClientFactory httpClientFactory, AppDbContext context,
            IWebHostEnvironment environment, IHubContext<RouteHub> hubContext)
        {
            _httpClientFactory = httpClientFactory;
            _context = context;
            _environment = environment;
            _hubContext = hubContext;
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

            // Group by coordinates and select only unique points
            var points = collections
                .GroupBy(c => new { c.Latitude, c.Longitude })
                .Select(g => new TraseuPoint
                {
                    NrMasina = "SB 77 ULB",
                    IdPubela = string.Join(", ", g.Select(c => c.IdPubela).Distinct()),
                    ColectatLa = g.First().CollectedAt,
                    Adresa = g.First().Adresa,
                    Latitude = g.Key.Latitude,
                    Longitude = g.Key.Longitude,
                    PunctCount = g.Count() // Adăugăm numărul de pubele la acest punct
                }).ToList();

            ViewBag.AvailableDates = availableDates;
            ViewBag.SelectedDate = date;
            ViewBag.TotalLines = points.Count;
            ViewBag.PointCount = points.Count;
            ViewBag.FirstPoint = points.FirstOrDefault();
            ViewBag.LastPoint = points.LastOrDefault();
            ViewBag.TotalCollections = collections.Count;

            return View(points);
        }

        [HttpPost]
        public async Task<IActionResult> OptimizeRoute([FromBody] JsonElement selectedDateElement)
        {
            try
            {
                var selectedDate = selectedDateElement.GetString();
                if (selectedDate == null)
                {
                    return Json(new { success = false, message = "Data selectată este invalidă!" });
                }

                var date = DateTime.Parse(selectedDate);
                var collections = await _context.Colectari
                    .Include(c => c.Pubela)
                    .Where(c => c.CollectedAt.Date == date.Date)
                    .OrderBy(c => c.CollectedAt)
                    .ToListAsync();

                if (!collections.Any())
                {
                    return Json(new { success = false, message = "Nu există colectări pentru data selectată!" });
                }

                await _hubContext.Clients.All.SendAsync("ReceiveRouteUpdate", $"Număr total colectări: {collections.Count}");

                // Group by coordinates and select only unique points
                var points = collections
                    .GroupBy(c => new { c.Latitude, c.Longitude })
                    .Select(g => new TraseuPoint
                    {
                        NrMasina = "SB 77 ULB",
                        IdPubela = string.Join(", ", g.Select(c => c.IdPubela).Distinct()),
                        ColectatLa = g.First().CollectedAt,
                        Adresa = g.First().Adresa,
                        Latitude = g.Key.Latitude,
                        Longitude = g.Key.Longitude,
                        PunctCount = g.Count()
                    }).ToList();

                await _hubContext.Clients.All.SendAsync("ReceiveRouteUpdate", $"Număr puncte unice: {points.Count}");

                var matrixFilePath = Path.Combine(_environment.ContentRootPath, "route_processing", "matrix_results", "full_matrix_mapbox.json");

                if (!System.IO.File.Exists(matrixFilePath))
                {
                    return Json(new { success = false, message = "Fișierul cu matricea de distanțe nu există!" });
                }

                // Read the distance matrix for original distance calculation
                var jsonString = System.IO.File.ReadAllText(matrixFilePath);

                try
                {
                    using (JsonDocument document = JsonDocument.Parse(jsonString))
                    {
                        var root = document.RootElement;
                        
                        if (!root.TryGetProperty("distances", out var distances))
                        {
                            return Json(new { success = false, message = "Formatul matricei de distanțe este invalid! Lipsește proprietatea 'distances'." });
                        }

                        if (!root.TryGetProperty("durations", out var durations))
                        {
                            return Json(new { success = false, message = "Formatul matricei de distanțe este invalid! Lipsește proprietatea 'durations'." });
                        }

                        if (distances.ValueKind != JsonValueKind.Array)
                        {
                            return Json(new { success = false, message = "Formatul matricei de distanțe este invalid! Proprietatea 'distances' nu este un array." });
                        }

                        var distanceMatrix = new List<List<int>>();
                        var durationMatrix = new List<List<int>>();
                        var rowCount = 0;
                        foreach (var row in distances.EnumerateArray())
                        {
                            if (row.ValueKind != JsonValueKind.Array)
                            {
                                return Json(new { success = false, message = $"Formatul matricei de distanțe este invalid! Rândul {rowCount} nu este un array." });
                            }

                            var distanceRow = new List<int>();
                            var colCount = 0;
                            foreach (var cell in row.EnumerateArray())
                            {
                                try
                                {
                                    distanceRow.Add((int)Math.Round(cell.GetDouble()));
                                    colCount++;
                                }
                                catch (Exception ex)
                                {
                                    return Json(new { success = false, message = $"Eroare la citirea distanței [{rowCount},{colCount}]: {ex.Message}" });
                                }
                            }
                            distanceMatrix.Add(distanceRow);
                            rowCount++;
                        }

                        // Citim matricea de durată
                        rowCount = 0;
                        foreach (var row in durations.EnumerateArray())
                        {
                            var durationRow = new List<int>();
                            var colCount = 0;
                            foreach (var cell in row.EnumerateArray())
                            {
                                try
                                {
                                    durationRow.Add((int)Math.Round(cell.GetDouble()));
                                    colCount++;
                                }
                                catch (Exception ex)
                                {
                                    return Json(new { success = false, message = $"Eroare la citirea duratei [{rowCount},{colCount}]: {ex.Message}" });
                                }
                            }
                            durationMatrix.Add(durationRow);
                            rowCount++;
                        }

                        if (points.Count > distanceMatrix.Count)
                        {
                            return Json(new { success = false, message = $"Numărul de puncte ({points.Count}) este mai mare decât dimensiunea matricei de distanțe ({distanceMatrix.Count})!" });
                        }

                        var optimizer = new LocalRouteOptimizer(matrixFilePath, _hubContext);
                        var (optimizedIndices, totalDistance, totalDuration) = await optimizer.OptimizeRoute();

                        if (optimizedIndices == null || !optimizedIndices.Any())
                        {
                            return Json(new { success = false, message = "Nu s-a putut genera un traseu optimizat!" });
                        }

                        var optimizedPoints = new List<TraseuPoint>();
                        foreach (var index in optimizedIndices.Take(points.Count))
                        {
                            if (index >= 0 && index < points.Count)
                            {
                                optimizedPoints.Add(points[index]);
                            }
                        }

                        if (!optimizedPoints.Any())
                        {
                            return Json(new { success = false, message = "Nu s-au putut mapa punctele optimizate!" });
                        }

                        // Calculate distances
                        var originalDistance = CalculateTotalMatrixDistance(points.Count, distanceMatrix);
                        var originalDuration = CalculateTotalMatrixDuration(points.Count, durationMatrix);
                        var originalDurationTimeSpan = TimeSpan.FromSeconds(originalDuration);
                        
                        var optimizedDistance = totalDistance / 1000.0; // Convert to kilometers
                        var distanceDifference = originalDistance - optimizedDistance;
                        var distanceReduction = originalDistance > 0 ? (distanceDifference / originalDistance) * 100 : 0;
                        
                        var durationDifference = originalDuration - totalDuration;
                        var durationReduction = originalDuration > 0 ? (durationDifference / originalDuration) * 100 : 0;
                        var estimatedDuration = TimeSpan.FromSeconds(totalDuration);

                        return Json(new
                        {
                            success = true,
                            optimizedRoute = optimizedPoints,
                            originalDistance,
                            optimizedDistance,
                            distanceDifference,
                            distanceReduction = Math.Round(distanceReduction, 2),
                            originalDuration = originalDurationTimeSpan.ToString(@"hh\:mm\:ss"),
                            estimatedDuration = estimatedDuration.ToString(@"hh\:mm\:ss"),
                            durationDifference = TimeSpan.FromSeconds(durationDifference).ToString(@"hh\:mm\:ss"),
                            durationReduction = Math.Round(durationReduction, 2)
                        });
                    }
                }
                catch (JsonException ex)
                {
                    return Json(new { success = false, message = $"Eroare la citirea matricei de distanțe: {ex.Message}" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Eroare la optimizarea traseului: {ex.Message}" });
            }
        }

        private double CalculateTotalMatrixDistance(List<int> route, List<List<int>>? matrix)
        {
            if (matrix == null || route.Count < 2) return 0;
            
            double totalDistance = 0;
            try
            {
                // Add distance from start point (index 0 in matrix) to first point
                if (route[0] >= 0 && route[0] < matrix[0].Count)
                {
                    Console.WriteLine($"Distanță start -> primul punct: {matrix[0][route[0]]} metri");
                    totalDistance += matrix[0][route[0]];
                }

                // Add distances between points
                for (int i = 0; i < route.Count - 1; i++)
                {
                    if (route[i] >= 0 && route[i] < matrix.Count && 
                        route[i + 1] >= 0 && route[i + 1] < matrix[route[i]].Count)
                    {
                        var distance = matrix[route[i]][route[i + 1]];
                        Console.WriteLine($"Distanță punct {i} -> punct {i+1}: {distance} metri");
                        totalDistance += distance;
                    }
                }

                // Add distance from last point to end point (last index in matrix)
                if (route.Last() >= 0 && route.Last() < matrix.Count)
                {
                    var lastDistance = matrix[route.Last()][matrix.Count - 1];
                    Console.WriteLine($"Distanță ultimul punct -> final: {lastDistance} metri");
                    totalDistance += lastDistance;
                }

                Console.WriteLine($"Distanță totală calculată: {totalDistance} metri = {totalDistance/1000.0:F2} km");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calculating distance: {ex.Message}");
                return 0;
            }

            return totalDistance / 1000.0; // Convert to kilometers
        }

        private double CalculateTotalMatrixDistance(int pointCount, List<List<int>>? matrix)
        {
            if (matrix == null || pointCount < 2) return 0;
            
            double totalDistance = 0;
            try
            {
                // Add distance from start point (index 0) to first collection point
                var startDistance = matrix[0][1];
                Console.WriteLine($"Distanță originală start -> primul punct: {startDistance} metri");
                totalDistance += startDistance;

                // Add distances between collection points
                for (int i = 1; i < pointCount - 1; i++)
                {
                    if (i + 1 < matrix[i].Count)
                    {
                        var distance = matrix[i][i + 1];
                        Console.WriteLine($"Distanță originală punct {i} -> punct {i+1}: {distance} metri");
                        totalDistance += distance;
                    }
                }

                // Add distance from last collection point to end point
                var endDistance = matrix[pointCount - 1][matrix.Count - 1];
                Console.WriteLine($"Distanță originală ultimul punct -> final: {endDistance} metri");
                totalDistance += endDistance;

                Console.WriteLine($"Distanță totală originală calculată: {totalDistance} metri = {totalDistance/1000.0:F2} km");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calculating distance: {ex.Message}");
                return 0;
            }

            return totalDistance / 1000.0; // Convert to kilometers
        }

        private double CalculateTotalMatrixDuration(int pointCount, List<List<int>>? durationMatrix)
        {
            if (durationMatrix == null || pointCount < 2) return 0;
            
            double totalDuration = 0;
            try
            {
                // Add duration from start point (index 0) to first collection point
                var startDuration = durationMatrix[0][1];
                Console.WriteLine($"Durată originală start -> primul punct: {startDuration} secunde");
                totalDuration += startDuration;

                // Add durations between collection points plus collection time
                for (int i = 1; i < pointCount - 1; i++)
                {
                    if (i + 1 < durationMatrix[i].Count)
                    {
                        var duration = durationMatrix[i][i + 1];
                        Console.WriteLine($"Durată originală punct {i} -> punct {i+1}: {duration} secunde");
                        totalDuration += duration;
                        
                        // Adăugăm 30 secunde pentru fiecare colectare
                        totalDuration += 30;
                        Console.WriteLine($"Adăugare timp colectare la punctul {i}: 30 secunde");
                    }
                }

                // Add duration from last collection point to end point
                var endDuration = durationMatrix[pointCount - 1][durationMatrix.Count - 1];
                Console.WriteLine($"Durată originală ultimul punct -> final: {endDuration} secunde");
                totalDuration += endDuration;

                Console.WriteLine($"Durată totală originală calculată: {totalDuration} secunde = {TimeSpan.FromSeconds(totalDuration)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calculating duration: {ex.Message}");
                return 0;
            }

            return totalDuration;
        }

        private async Task<List<TraseuPoint>> GetPointsFromCsv(string filePath)
        {
            var points = new List<TraseuPoint>();

            using (var reader = new StreamReader(filePath))
            using (var csv = new CsvReader(reader,
                       new CsvConfiguration(CultureInfo.InvariantCulture) { Delimiter = "," }))
            {
                await foreach (var record in csv.GetRecordsAsync<TraseuPoint>())
                {
                    points.Add(record);
                }
            }

            return points;
        }

        private async Task<TraseuPoint> GetCoordinates(string address)
        {
            var client = _httpClientFactory.CreateClient();
            var response =
                await client.GetAsync(
                    $"https://maps.googleapis.com/maps/api/geocode/json?address={address}&key=YOUR_API_KEY");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var geocodeResponse = JsonSerializer.Deserialize<GeocodeResponse>(content);

            if (geocodeResponse?.Results.FirstOrDefault() != null)
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