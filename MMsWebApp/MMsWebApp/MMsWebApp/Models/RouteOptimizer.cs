using Google.OrTools.ConstraintSolver;
using System.Text.Json;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.SignalR;
using MMsWebApp.Hubs;

namespace MMsWebApp.Models
{
    public class RouteOptimizer
    {
        private readonly int[,] distanceMatrix;
        private readonly int[,] durationMatrix;
        private readonly int vehicleNumber = 1;
        private readonly int depot = 0;
        private readonly IHubContext<RouteHub> _hubContext;

        public RouteOptimizer(string matrixFilePath, IHubContext<RouteHub> hubContext)
        {
            _hubContext = hubContext;
            try
            {
                Console.WriteLine("Inițializare RouteOptimizer...");
                _hubContext.Clients.All.SendAsync("ReceiveRouteUpdate", "Inițializare RouteOptimizer...");
                
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
                    {
                        throw new InvalidOperationException("Matricea de distanțe este goală!");
                    }

                    var firstRow = distances.EnumerateArray().First();
                    var cols = firstRow.GetArrayLength();

                    if (rows != cols)
                    {
                        throw new InvalidOperationException($"Matricea de distanțe nu este pătrată! ({rows}x{cols})");
                    }

                    distanceMatrix = new int[rows, cols];
                    durationMatrix = new int[rows, cols];
                    var rowIndex = 0;
                    
                    // Citim distanțele
                    foreach (var row in distances.EnumerateArray())
                    {
                        var colIndex = 0;
                        foreach (var cell in row.EnumerateArray())
                        {
                            var distance = (int)Math.Round(cell.GetDouble());
                            distanceMatrix[rowIndex, colIndex] = distance;
                            colIndex++;
                        }
                        rowIndex++;
                    }

                    // Citim duratele
                    rowIndex = 0;
                    foreach (var row in durations.EnumerateArray())
                    {
                        var colIndex = 0;
                        foreach (var cell in row.EnumerateArray())
                        {
                            var duration = (int)Math.Round(cell.GetDouble());
                            durationMatrix[rowIndex, colIndex] = duration;
                            colIndex++;
                        }
                        rowIndex++;
                    }
                    _hubContext.Clients.All.SendAsync("ReceiveRouteUpdate", "Matrice de distanțe și durate încărcată cu succes.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Eroare la inițializarea RouteOptimizer: {ex.Message}");
                _hubContext.Clients.All.SendAsync("ReceiveRouteUpdate", $"Eroare la inițializarea RouteOptimizer: {ex.Message}");
                throw;
            }
        }

        public async Task<(List<int> Route, double TotalDistance, double TotalDuration)> OptimizeRoute()
        {
            try
            {
                await _hubContext.Clients.All.SendAsync("ReceiveRouteUpdate", "Începere optimizare traseu...");
                var size = distanceMatrix.GetLength(0);

                // Create Routing Index Manager
                RoutingIndexManager manager = new(size, vehicleNumber, depot);
                await _hubContext.Clients.All.SendAsync("ReceiveRouteUpdate", "Manager de rutare creat.");

                // Create Routing Model
                RoutingModel routing = new(manager);
                await _hubContext.Clients.All.SendAsync("ReceiveRouteUpdate", "Model de rutare creat.");

                // Create and register a transit callback
                int transitCallbackIndex = routing.RegisterTransitCallback(
                    (long fromIndex, long toIndex) =>
                    {
                        var fromNode = manager.IndexToNode(fromIndex);
                        var toNode = manager.IndexToNode(toIndex);
                        var distance = distanceMatrix[fromNode, toNode];
                        
                        // Trimitem informații despre verificarea distanței
                        _hubContext.Clients.All.SendAsync("ReceiveDistanceCheck", fromNode, toNode, distance).Wait();
                        
                        return distance;
                    });

                // Define cost of each arc
                routing.SetArcCostEvaluatorOfAllVehicles(transitCallbackIndex);

                // Setting first solution heuristic
                RoutingSearchParameters searchParameters =
                    operations_research_constraint_solver.DefaultRoutingSearchParameters();
                searchParameters.FirstSolutionStrategy = FirstSolutionStrategy.Types.Value.PathCheapestArc;
                searchParameters.LocalSearchMetaheuristic = LocalSearchMetaheuristic.Types.Value.GuidedLocalSearch;
                searchParameters.TimeLimit = new Duration { Seconds = 30 };
                await _hubContext.Clients.All.SendAsync("ReceiveRouteUpdate", "Parametri de căutare configurați.");

                // Solve the problem
                Assignment solution = routing.SolveWithParameters(searchParameters);
                if (solution == null)
                {
                    await _hubContext.Clients.All.SendAsync("ReceiveRouteUpdate", "Nu s-a găsit nicio soluție!");
                    return (new List<int>(), 0, 0);
                }
                await _hubContext.Clients.All.SendAsync("ReceiveRouteUpdate", "Soluție găsită.");

                // Get the optimized route
                List<int> route = new();
                double totalDistance = 0;
                double totalDuration = 0;
                long index = routing.Start(0);
                
                // Add start point to depot distance
                var firstNode = manager.IndexToNode(solution.Value(routing.NextVar(index)));
                totalDistance += distanceMatrix[0, firstNode];
                totalDuration += durationMatrix[0, firstNode];
                await _hubContext.Clients.All.SendAsync("ReceiveRouteUpdate", $"Adăugare segment: Depozit -> Punct {firstNode}");

                while (!routing.IsEnd(index))
                {
                    var nodeIndex = manager.IndexToNode(index);
                    route.Add(nodeIndex);
                    var nextIndex = solution.Value(routing.NextVar(index));
                    var nextNode = manager.IndexToNode(nextIndex);
                    
                    var segmentDistance = distanceMatrix[nodeIndex, nextNode];
                    var segmentDuration = durationMatrix[nodeIndex, nextNode];
                    
                    // Add collection time (30 seconds per bin)
                    var collectionTime = 30.0; // 30 seconds base time per stop
                    totalDistance += segmentDistance;
                    totalDuration += segmentDuration + collectionTime;
                    
                    await _hubContext.Clients.All.SendAsync("ReceiveRouteUpdate", 
                        $"Adăugare segment: Punct {nodeIndex} -> Punct {nextNode} ({segmentDistance}m, {segmentDuration + collectionTime}s)");
                    
                    index = nextIndex;
                }

                // Add last point to depot distance
                var lastNode = manager.IndexToNode(index);
                route.Add(lastNode);
                totalDistance += distanceMatrix[lastNode, 0];
                totalDuration += durationMatrix[lastNode, 0];
                await _hubContext.Clients.All.SendAsync("ReceiveRouteUpdate", $"Adăugare segment final: Punct {lastNode} -> Depozit");

                await _hubContext.Clients.All.SendAsync("ReceiveRouteUpdate", 
                    $"Traseu optimizat generat cu {route.Count} puncte. " +
                    $"Distanță totală: {totalDistance/1000.0:F2} km, " +
                    $"Durată totală: {TimeSpan.FromSeconds(totalDuration)}");
                
                return (route, totalDistance, totalDuration);
            }
            catch (Exception ex)
            {
                await _hubContext.Clients.All.SendAsync("ReceiveRouteUpdate", $"Eroare la optimizarea traseului: {ex.Message}");
                throw;
            }
        }
    }
} 