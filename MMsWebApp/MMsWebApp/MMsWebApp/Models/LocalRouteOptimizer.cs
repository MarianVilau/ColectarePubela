using Microsoft.AspNetCore.SignalR;
using MMsWebApp.Hubs;
using System.Text.Json;

namespace MMsWebApp.Models
{
    public class LocalRouteOptimizer
    {
        private readonly int[,] distanceMatrix;
        private readonly int[,] durationMatrix;
        private readonly IHubContext<RouteHub> _hubContext;
        private readonly Random random = new Random();
        
        // Parametri pentru algoritmul genetic
        private const int POPULATION_SIZE = 100;
        private const int MAX_GENERATIONS = 100;
        private const double MUTATION_RATE = 0.015;
        private const int TOURNAMENT_SIZE = 5;
        private const int ELITE_SIZE = 10;

        public LocalRouteOptimizer(string matrixFilePath, IHubContext<RouteHub> hubContext)
        {
            _hubContext = hubContext;
            try
            {
                Console.WriteLine("Inițializare LocalRouteOptimizer...");
                _hubContext.Clients.All.SendAsync("ReceiveRouteUpdate", "Inițializare LocalRouteOptimizer...");
                
                var jsonString = File.ReadAllText(matrixFilePath);
                using (JsonDocument document = JsonDocument.Parse(jsonString))
                {
                    var root = document.RootElement;
                    var distances = root.GetProperty("distances");
                    var durations = root.GetProperty("durations");
                    var rows = distances.GetArrayLength();
                    
                    Console.WriteLine($"Număr rânduri în matrice: {rows}");
                    _hubContext.Clients.All.SendAsync("ReceiveRouteUpdate", $"Număr rânduri în matrice: {rows}");

                    if (rows == 0)
                        throw new InvalidOperationException("Matricea de distanțe este goală!");

                    var firstRow = distances.EnumerateArray().First();
                    var cols = firstRow.GetArrayLength();

                    if (rows != cols)
                        throw new InvalidOperationException($"Matricea de distanțe nu este pătrată! ({rows}x{cols})");

                    distanceMatrix = new int[rows, cols];
                    durationMatrix = new int[rows, cols];
                    
                    // Citim distanțele și duratele
                    LoadMatrixData(distances, durations);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Eroare la inițializarea LocalRouteOptimizer: {ex.Message}");
                _hubContext.Clients.All.SendAsync("ReceiveRouteUpdate", $"Eroare la inițializarea LocalRouteOptimizer: {ex.Message}");
                throw;
            }
        }

        private void LoadMatrixData(JsonElement distances, JsonElement durations)
        {
            var rowIndex = 0;
            foreach (var row in distances.EnumerateArray())
            {
                var colIndex = 0;
                foreach (var cell in row.EnumerateArray())
                {
                    distanceMatrix[rowIndex, colIndex] = (int)Math.Round(cell.GetDouble());
                    colIndex++;
                }
                rowIndex++;
            }

            rowIndex = 0;
            foreach (var row in durations.EnumerateArray())
            {
                var colIndex = 0;
                foreach (var cell in row.EnumerateArray())
                {
                    durationMatrix[rowIndex, colIndex] = (int)Math.Round(cell.GetDouble());
                    colIndex++;
                }
                rowIndex++;
            }
        }

        public async Task<(List<int> Route, double TotalDistance, double TotalDuration)> OptimizeRoute()
        {
            try
            {
                await _hubContext.Clients.All.SendAsync("ReceiveRouteUpdate", "Începere optimizare traseu folosind Algoritm Genetic...");
                
                var size = distanceMatrix.GetLength(0);
                var population = InitializePopulation(size);
                var bestRoute = new List<int>();
                var bestDistance = double.MaxValue;
                var bestDuration = 0.0;
                var generationsWithoutImprovement = 0;

                for (int generation = 0; generation < MAX_GENERATIONS; generation++)
                {
                    // Sortăm populația după fitness (distanța totală)
                    population.Sort((a, b) => CalculateDistance(a).CompareTo(CalculateDistance(b)));

                    // Verificăm dacă avem o îmbunătățire
                    var currentBestDistance = CalculateDistance(population[0]);
                    if (currentBestDistance < bestDistance)
                    {
                        bestDistance = currentBestDistance;
                        bestRoute = new List<int>(population[0]);
                        bestDuration = CalculateDuration(bestRoute);
                        generationsWithoutImprovement = 0;
                        
                        await _hubContext.Clients.All.SendAsync("ReceiveRouteUpdate", 
                            $"Generația {generation}: Nou traseu optim găsit! Distanță: {bestDistance/1000.0:F2} km");
                    }
                    else
                    {
                        generationsWithoutImprovement++;
                        if (generationsWithoutImprovement > 20) // Early stopping
                            break;
                    }

                    // Creăm noua generație
                    var newPopulation = new List<List<int>>();

                    // Păstrăm elitele
                    for (int i = 0; i < ELITE_SIZE; i++)
                        newPopulation.Add(new List<int>(population[i]));

                    // Completăm restul populației prin selecție și încrucișare
                    while (newPopulation.Count < POPULATION_SIZE)
                    {
                        var parent1 = TournamentSelection(population);
                        var parent2 = TournamentSelection(population);
                        var (child1, child2) = CrossOver(parent1, parent2);
                        
                        Mutate(child1);
                        Mutate(child2);

                        newPopulation.Add(child1);
                        if (newPopulation.Count < POPULATION_SIZE)
                            newPopulation.Add(child2);
                    }

                    population = newPopulation;
                }

                await _hubContext.Clients.All.SendAsync("ReceiveRouteUpdate", 
                    $"Optimizare finalizată. Distanță totală: {bestDistance/1000.0:F2} km, " +
                    $"Durată totală: {TimeSpan.FromSeconds(bestDuration)}");

                return (bestRoute, bestDistance, bestDuration);
            }
            catch (Exception ex)
            {
                await _hubContext.Clients.All.SendAsync("ReceiveRouteUpdate", $"Eroare la optimizarea traseului: {ex.Message}");
                throw;
            }
        }

        private List<List<int>> InitializePopulation(int size)
        {
            var population = new List<List<int>>();
            
            for (int i = 0; i < POPULATION_SIZE; i++)
            {
                var route = Enumerable.Range(1, size - 2).ToList(); // Excludem start (0) și stop (size-1)
                for (int j = route.Count - 1; j > 0; j--)
                {
                    var k = random.Next(j + 1);
                    (route[j], route[k]) = (route[k], route[j]); // Shuffle
                }
                route.Insert(0, 0); // Adăugăm start
                route.Add(size - 1); // Adăugăm stop
                population.Add(route);
            }
            
            return population;
        }

        private double CalculateDistance(List<int> route)
        {
            double distance = 0;
            for (int i = 0; i < route.Count - 1; i++)
                distance += distanceMatrix[route[i], route[i + 1]];
            return distance;
        }

        private double CalculateDuration(List<int> route)
        {
            double duration = 0;
            for (int i = 0; i < route.Count - 1; i++)
            {
                duration += durationMatrix[route[i], route[i + 1]];
                if (i > 0 && i < route.Count - 2) // Adăugăm timp de colectare pentru punctele intermediare
                    duration += 30;
            }
            return duration;
        }

        private List<int> TournamentSelection(List<List<int>> population)
        {
            var tournament = new List<List<int>>();
            for (int i = 0; i < TOURNAMENT_SIZE; i++)
            {
                var randomIndex = random.Next(population.Count);
                tournament.Add(population[randomIndex]);
            }
            
            tournament.Sort((a, b) => CalculateDistance(a).CompareTo(CalculateDistance(b)));
            return new List<int>(tournament[0]);
        }

        private (List<int>, List<int>) CrossOver(List<int> parent1, List<int> parent2)
        {
            var size = parent1.Count;
            var start = 1; // Păstrăm punctul de start
            var end = size - 2; // Păstrăm punctul de stop
            
            var point1 = random.Next(start, end);
            var point2 = random.Next(point1 + 1, end + 1);

            var child1 = CreateChild(parent1, parent2, point1, point2);
            var child2 = CreateChild(parent2, parent1, point1, point2);

            return (child1, child2);
        }

        private List<int> CreateChild(List<int> parent1, List<int> parent2, int point1, int point2)
        {
            var size = parent1.Count;
            var child = new List<int>(new int[size]);
            
            // Copiem segmentul din primul părinte
            child[0] = 0; // Start fix
            child[size - 1] = size - 1; // Stop fix
            for (int i = point1; i <= point2; i++)
                child[i] = parent1[i];

            // Completăm restul pozițiilor cu gene din al doilea părinte
            var remaining = parent2.Skip(1).Take(parent2.Count - 2)
                                 .Where(x => !child.Skip(point1).Take(point2 - point1 + 1).Contains(x))
                                 .ToList();
            
            var index = 0;
            for (int i = 1; i < size - 1; i++)
            {
                if (i < point1 || i > point2)
                {
                    child[i] = remaining[index++];
                }
            }

            return child;
        }

        private void Mutate(List<int> route)
        {
            if (random.NextDouble() < MUTATION_RATE)
            {
                // Selectăm două poziții aleatoare (excluzând start și stop)
                var pos1 = random.Next(1, route.Count - 2);
                var pos2 = random.Next(1, route.Count - 2);
                
                // Swap
                (route[pos1], route[pos2]) = (route[pos2], route[pos1]);
            }
        }
    }
} 