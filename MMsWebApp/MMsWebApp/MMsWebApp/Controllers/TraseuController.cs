using Microsoft.AspNetCore.Mvc;
using MMsWebApp.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MMsWebApp.Controllers
{
    public class TraseuController : Controller
    {
        public IActionResult Index()
        {
            ViewBag.Debug = new List<string>();
            var traseuPoints = LoadTraseuData();

            if (traseuPoints == null)
            {
                ViewBag.Error = "Lista de puncte este null.";
                return View(new List<TraseuPoint>());
            }

            ViewBag.Debug.Add($"Număr total de puncte încărcate: {traseuPoints.Count}");

            if (!traseuPoints.Any())
            {
                ViewBag.Error = "Nu s-au găsit date pentru traseu.";
                return View(new List<TraseuPoint>());
            }

            ViewBag.PointCount = traseuPoints.Count;
            ViewBag.FirstPoint = traseuPoints.First();
            ViewBag.LastPoint = traseuPoints.Last();
            return View(traseuPoints);
        }

        private List<TraseuPoint> LoadTraseuData()
        {
            var traseuPoints = new List<TraseuPoint>();
            var debugMessages = ViewBag.Debug as List<string>;

            // Verificăm calea fișierului
            var currentDir = Directory.GetCurrentDirectory();
            debugMessages.Add($"Director curent: {currentDir}");

            var filePath = Path.Combine(currentDir, "Data", "Traseu2.csv");
            debugMessages.Add($"Cale fișier: {filePath}");

            if (!System.IO.File.Exists(filePath))
            {
                debugMessages.Add("EROARE: Fișierul nu există!");
                ViewBag.Error = $"Fișierul nu a fost găsit la calea: {filePath}";
                return traseuPoints;
            }

            debugMessages.Add("Fișierul există, începem citirea...");

            try
            {
                var lines = System.IO.File.ReadAllLines(filePath);
                debugMessages.Add($"Linii citite din fișier: {lines.Length}");
                ViewBag.TotalLines = lines.Length;

                if (lines.Length <= 1)
                {
                    debugMessages.Add("EROARE: Fișier gol sau doar cu header");
                    ViewBag.Error = "Fișierul este gol sau conține doar header.";
                    return traseuPoints;
                }

                var validLines = 0;
                var invalidLines = 0;
                var processedLines = 0;

                foreach (var line in lines.Skip(1)) // Skip header
                {
                    processedLines++;
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        debugMessages.Add($"Linia {processedLines} este goală, skip");
                        continue;
                    }

                    // Fixarea problemei cu delimitatorul și ghilimelele din CSV
                    var values = ParseCSVLine(line);

                    if (values.Length < 6)
                    {
                        debugMessages.Add($"Linia {processedLines} are mai puțin de 6 valori: {line}");
                        invalidLines++;
                        continue;
                    }

                    // Skip empty lines
                    if (string.IsNullOrWhiteSpace(values[0]) && string.IsNullOrWhiteSpace(values[1]) &&
                        string.IsNullOrWhiteSpace(values[2]))
                    {
                        debugMessages.Add($"Linia {processedLines} are primele 3 câmpuri goale, skip");
                        continue;
                    }

                    try
                    {
                        var dateStr = values[2]?.Trim() ?? string.Empty;
                        var latStr = values[4]?.Trim() ?? string.Empty;
                        var lngStr = values[5]?.Trim() ?? string.Empty;

                        debugMessages.Add($"Procesăm linia {processedLines}:");
                        debugMessages.Add($"  Data: {dateStr}");
                        debugMessages.Add($"  Lat: {latStr}");
                        debugMessages.Add($"  Lng: {lngStr}");

                        var point = new TraseuPoint
                        {
                            NrMasina = values[0]?.Trim() ?? string.Empty,
                            IdPubela = values[1]?.Trim() ?? string.Empty,
                            ColectatLa = ParseDateTime(dateStr),
                            Adresa = values[3]?.Trim() ?? string.Empty,
                            Latitude = ParseDouble(latStr),
                            Longitude = ParseDouble(lngStr)
                        };

                        debugMessages.Add($"  Punct creat: {point.NrMasina}, {point.IdPubela}, {point.ColectatLa}");

                        // Verify if we have valid coordinates and date
                        if (point.Latitude != 0 && point.Longitude != 0 && point.ColectatLa != DateTime.MinValue)
                        {
                            traseuPoints.Add(point);
                            validLines++;
                            debugMessages.Add($"  Punct valid adăugat!");
                        }
                        else
                        {
                            debugMessages.Add($"  Punct invalid: Lat={point.Latitude}, Lng={point.Longitude}, Data={point.ColectatLa}");
                            invalidLines++;
                        }
                    }
                    catch (Exception ex)
                    {
                        debugMessages.Add($"EROARE la linia {processedLines}: {ex.Message}");
                        invalidLines++;
                    }
                }

                debugMessages.Add($"Sumar procesare:");
                debugMessages.Add($"  Total linii procesate: {processedLines}");
                debugMessages.Add($"  Linii valide: {validLines}");
                debugMessages.Add($"  Linii invalide: {invalidLines}");
            }
            catch (Exception ex)
            {
                debugMessages.Add($"EROARE CRITICĂ la citirea fișierului: {ex.Message}");
                ViewBag.Error = $"Eroare la citirea fișierului: {ex.Message}";
                return traseuPoints;
            }

            if (!traseuPoints.Any())
            {
                debugMessages.Add("Nu s-au găsit puncte valide în fișier!");
                ViewBag.Error = "Nu s-au putut încărca puncte valide din fișier.";
            }

            return traseuPoints;
        }

        private DateTime ParseDateTime(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                ViewBag.Debug.Add($"ParseDateTime: valoare goală");
                return DateTime.MinValue;
            }

            value = value.Trim('"');
            ViewBag.Debug.Add($"ParseDateTime: procesăm '{value}'");

            try
            {
                // Încercăm să parsăm direct ca ISO 8601
                DateTime result = DateTime.Parse(value, null, System.Globalization.DateTimeStyles.RoundtripKind);
                ViewBag.Debug.Add($"ParseDateTime: convertit ISO8601 '{value}' la local '{result.ToLocalTime()}'");
                return result.ToLocalTime();
            }
            catch (Exception ex)
            {
                ViewBag.Debug.Add($"ParseDateTime: eroare la parsare ISO8601: {ex.Message}");

                // Încercăm formatele alternative
                if (DateTime.TryParse(value, out DateTime fallbackResult))
                {
                    ViewBag.Debug.Add($"ParseDateTime: convertit folosind format alternativ '{value}' la '{fallbackResult}'");
                    return fallbackResult;
                }
            }

            ViewBag.Debug.Add($"ParseDateTime: nu s-a putut converti '{value}'");
            return DateTime.MinValue;
        }

        private double ParseDouble(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                ViewBag.Debug.Add($"ParseDouble: valoare goală");
                return 0;
            }

            value = value.Trim('"');
            ViewBag.Debug.Add($"ParseDouble: procesăm '{value}'");

            if (double.TryParse(value, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out double result))
            {
                ViewBag.Debug.Add($"ParseDouble: convertit '{value}' la {result}");
                return result;
            }

            ViewBag.Debug.Add($"ParseDouble: nu s-a putut converti '{value}'");
            return 0;
        }

        private string[] ParseCSVLine(string line)
        {
            List<string> result = new List<string>();
            bool inQuotes = false;
            string currentValue = "";

            foreach (char c in line)
            {
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                    continue;
                }

                if (c == ',' && !inQuotes)
                {
                    result.Add(currentValue);
                    currentValue = "";
                    continue;
                }

                currentValue += c;
            }

            // Adăugăm și ultima valoare
            result.Add(currentValue);

            return result.ToArray();
        }
    }
}