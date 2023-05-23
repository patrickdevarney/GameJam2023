using System.Collections.Generic;
using UnityEngine;
using Unity.Profiling;

namespace Navigation
{
    public class NavManager : MonoBehaviour
    {
        // Global movement cost from one node to another
        public const int MOVEMENT_COST = 1;

        static readonly ProfilerMarker GetWorldPathPerfMarker = new ProfilerMarker(ProfilerCategory.Ai, "GetWorldPath");
        static readonly ProfilerMarker EvaluateChildPerfMarker = new ProfilerMarker(ProfilerCategory.Ai, "Evaluate Child");
        static readonly ProfilerMarker GetPathPerfMarker = new ProfilerMarker(ProfilerCategory.Ai, "GetPath");

        public static NavManager Singleton;

        // Store x,y int pair
        public struct Coordinate
        {
            public int x;
            public int y;

            public Coordinate(int x, int y)
            {
                this.x = x;
                this.y = y;
            }

            public static bool operator ==(Coordinate a, Coordinate b)
            {
                return a.x == b.x &&
                    a.y == b.y;
            }

            public static bool operator !=(Coordinate a, Coordinate b)
            {
                return a.x != b.x ||
                    a.y != b.y;
            }

            public override string ToString()
            {
                return $"({x},{y})";
            }
        }

        // During pathfinding, store data about each NavNode
        struct PathfindingNode
        {
            public bool hasBeenVisited;
            public int costToReach;
            public Coordinate coordinate;
            // TODO: instead of storing coordinate, we could store a bit flag for NSWE. Reduce 8bytes to 4bits
            public Coordinate closestNodeToStart;
        }

        PathfindingNode[,] nodes;

        [Header("Bake settings")]
        public BakedNavData.BakeSettings settings = new BakedNavData.BakeSettings
        {
            width = 10,
            length = 10,
            distanceBetweenCells = 1
        };
        public bool autoUpdate;

        [Header("Used at runtime")]
        public BakedNavData navData;

        [Header("Gizmos")]
        public bool gizmoEnabled;
        public bool gizmoCulling;
        public float gizmoCullingDistance = 20f;

        // TODO: maybe we can wrap a request in a struct for storing additional data. Store (from,to) together
        private Coordinate currentDestinationRequest;

        private void OnValidate()
        {
            if (!autoUpdate)
            {
                return;
            }

            if (navData == null)
            {
                return;
            }

            if (navData.settings != settings)
            {
                Bake();
            }
        }

        private void OnEnable()
        {
            Singleton = this;
        }

        /// <summary>
        /// Bakes and stores data in linked data asset
        /// </summary>
        [ContextMenu("Bake")]
        public void Bake()
        {
            if (navData == null)
            {
                Debug.LogError("Bake aborted: no linked navData");
                return;
            }

            var timer = new System.Diagnostics.Stopwatch();
            // Copy the bake settings to the data object
            navData.settings = settings;
            // Create array of nodes
            navData.nodes = new BakedNavData.NavNode[settings.width * settings.length];
            // Customize each node (blockers, travel cost, etc)
            NavBlocker[] blockers = FindObjectsByType<NavBlocker>(FindObjectsSortMode.None);
            for (int i = 0; i < blockers.Length; i++)
            {
                if (!blockers[i].isStatic)
                {
                    continue;
                }

                // Find all nav points within bounds

                Vector3 blockerCenter = blockers[i].Center;
                Vector3 blockerExtents = blockers[i].extents;
                // Find the lower-left corner (lowest x,z value in Unity space)
                Vector3 lowerLeftCorner = blockerCenter - blockerExtents;
                // Convert to grid position
                Coordinate lowerLeftGridPosition = GetGridPositionFromWorldPosition(lowerLeftCorner);
                // Convert world length/width to grid length/width
                int blockerGridWidth = GetGridDistanceFromWorldDistance(blockers[i].extents.x * 2);
                int blockerGridLength = GetGridDistanceFromWorldDistance(blockers[i].extents.z * 2);
                // Loop through all grid points that are within this width/length starting at our lower-left corner
                for (int x = 0; x < blockerGridWidth; x++)
                {
                    for (int y = 0; y < blockerGridLength; y++)
                    {
                        if (!navData.IsWithinGrid(lowerLeftGridPosition.x + x, lowerLeftGridPosition.y + y))
                        {
                            continue;
                        }

                        navData[lowerLeftGridPosition.x + x, lowerLeftGridPosition.y + y].isBlocked = true;
                    }
                }
            }

            // Save changes
            UnityEditor.EditorUtility.SetDirty(navData);
            timer.Stop();
            Debug.Log($"Bake completed in {timer.ElapsedMilliseconds}ms");
        }

        public int testIterations = 1;
        [ContextMenu("Test random path")]
        public long TestRandomPath()
        {
            Coordinate from = new Coordinate
            {
                x = Random.Range(0, navData.settings.width),
                y = Random.Range(0, navData.settings.length),
            };

            Coordinate to = new Coordinate
            {
                x = Random.Range(0, navData.settings.width),
                y = Random.Range(0, navData.settings.length),
            };

            Vector3 worldFrom = GetWorldPositionFromGridPosition(from.x, from.y);
            Vector3 worldTo = GetWorldPositionFromGridPosition(to.x, to.y);

            var timer = new System.Diagnostics.Stopwatch();
            timer.Start();
            GetWorldPath(worldFrom, worldTo);
            timer.Stop();
            Debug.Log($"Time elapsed: {timer.ElapsedMilliseconds}ms");
            return timer.ElapsedMilliseconds;
        }

        [ContextMenu("Test multiple paths")]
        public void StartTest()
        {
            long totalElapsedMilliseconds = 0;
            for (int i = 0; i < testIterations; i++)
            {
                long elapsedMilliseconds = TestRandomPath();
                totalElapsedMilliseconds += elapsedMilliseconds;
            }
            Debug.Log($"Ran {testIterations} tests. Average test {totalElapsedMilliseconds / testIterations}ms");
        }

        public List<Vector3> GetWorldPath(Vector3 fromWorldPosition, Vector3 toWorldPosition)
        {
            using (GetWorldPathPerfMarker.Auto())
            {
                // Convert the requested positions into grid points
                Coordinate fromGridPosition = GetGridPositionFromWorldPosition(fromWorldPosition);
                Coordinate toGridPosition = GetGridPositionFromWorldPosition(toWorldPosition);

                // Check bounds
                if (!navData.IsWithinGrid(fromGridPosition))
                {
                    Debug.LogError($"Fail to path: start world position {fromWorldPosition} is not within the grid");
                    return null;
                }
                if (!navData.IsWithinGrid(toGridPosition))
                {
                    Debug.LogError($"Fail to path: target world position {toWorldPosition} is not within the grid");
                    return null;
                }

                // Check if we have already arrived at our destination
                if (fromGridPosition == toGridPosition)
                {
                    // Simply return the world position so we nudge towards it
                    return new List<Vector3>()
                        {
                            toWorldPosition
                        };
                }

                // Find the path
                GetPathPerfMarker.Begin();
                var gridPath = GetPath(fromGridPosition, toGridPosition);
                GetPathPerfMarker.End();

                if (gridPath == null)
                {
                    Debug.LogError($"Fail to path: could not find valid path");
                    return null;
                }

                // Convert the grid path to world positions
                List<Vector3> worldPath = new List<Vector3>(gridPath.Count);
                for (int i = 0; i < gridPath.Count; i++)
                {
                    worldPath.Add(GetWorldPositionFromGridPosition((int)gridPath[i].x, (int)gridPath[i].y));
                }

                // Append the final world position so that we smoothly land on it
                worldPath.Add(toWorldPosition);

                return worldPath;
            }
        }

        List<Coordinate> GetPath(Coordinate from, Coordinate to)
        {
            // As long as edges have uniform cost, A* should beat Dijkstra and still provide the shortest path

            // We have a 2D array of nodes
            // Elements can have neighbors north/south/east/west
            // Every edge has an equal travel cost

            // Create pathfinding data for each nav node
            if (nodes != null &&
                nodes.GetLength(0) == navData.settings.width &&
                nodes.GetLength(1) == navData.settings.length)
            {
                // We already have the correctly-sized array
                System.Array.Clear(nodes, 0, nodes.Length);
            }
            else
            {
                // Our previous pathfinding data is not correctly-size
                nodes = new PathfindingNode[navData.settings.width, navData.settings.length];
            }

            // Store which nodes are available
            List<Coordinate> sortedAvailableNodes = new List<Coordinate>();
            // Add the first node
            sortedAvailableNodes.Add(from);

            while (sortedAvailableNodes.Count > 0)
            {
                // Sort by known cost + shortest possible distance to finish

                //sortedAvailableNodes.Sort((b, a) => nodes[a.x, a.y].costToReach.CompareTo(nodes[b.x, b.y].costToReach));
                // Dequeue the last element (cheapest node to visit)
                Coordinate currentCoordinate = sortedAvailableNodes[sortedAvailableNodes.Count - 1];
                sortedAvailableNodes.RemoveAt(sortedAvailableNodes.Count - 1);

                // Check if destination was reached
                if (currentCoordinate == to)
                {
                    // Calculate shortest path
                    List<Coordinate> shortestPath = new List<Coordinate>();
                    shortestPath.Add(to);
                    BuildShortestPath(shortestPath, from, ref nodes);
                    shortestPath.Reverse();
                    return shortestPath;
                }

                // Evaluate each child (north, south, east, west)
                EvaluateChildNode(currentCoordinate, new Coordinate(currentCoordinate.x, currentCoordinate.y + 1), to, ref sortedAvailableNodes, ref nodes);
                EvaluateChildNode(currentCoordinate, new Coordinate(currentCoordinate.x, currentCoordinate.y - 1), to, ref sortedAvailableNodes, ref nodes);
                EvaluateChildNode(currentCoordinate, new Coordinate(currentCoordinate.x + 1, currentCoordinate.y), to, ref sortedAvailableNodes, ref nodes);
                EvaluateChildNode(currentCoordinate, new Coordinate(currentCoordinate.x - 1, currentCoordinate.y), to, ref sortedAvailableNodes, ref nodes);

                // Mark current node as visited
                nodes[currentCoordinate.x, currentCoordinate.y].hasBeenVisited = true;
            }

            // Failed to find path
            return null;
        }

        void EvaluateChildNode(Coordinate parentCoordinate, Coordinate childCoordinate, Coordinate destination, ref List<Coordinate> sortedAvailableNodes, ref PathfindingNode[,] nodes)
        {
            using (EvaluateChildPerfMarker.Auto())
            {
                // Check if this node exists
                if (!navData.IsWithinGrid(childCoordinate))
                {
                    return;
                }

                // Check if this node is pathable
                if (navData[childCoordinate.x, childCoordinate.y].isBlocked)
                {
                    return;
                }

                // Check if this node was already visited
                if (nodes[childCoordinate.x, childCoordinate.y].hasBeenVisited)
                {
                    return;
                }

                int recordedCostToGetHere = nodes[childCoordinate.x, childCoordinate.y].costToReach;
                int alternativeCostToGetHere = nodes[parentCoordinate.x, parentCoordinate.y].costToReach + MOVEMENT_COST;

                if (recordedCostToGetHere == 0 ||
                    recordedCostToGetHere > alternativeCostToGetHere)
                {
                    // We are either the first path to this node or a cheaper path
                    // Grab a copy of the node data
                    PathfindingNode childNode = nodes[childCoordinate.x, childCoordinate.y];
                    // Override data
                    childNode.costToReach = alternativeCostToGetHere;
                    childNode.closestNodeToStart = parentCoordinate;
                    childNode.coordinate = childCoordinate;
                    // Store data
                    nodes[childCoordinate.x, childCoordinate.y] = childNode;

                    // Add as a reachable node
                    if (!childNode.hasBeenVisited)
                    {
                        // Insert into the list sorted by distance to target
                        // Store destination to avoid closure
                        currentDestinationRequest = destination;
                        sortedAvailableNodes.InsertIntoSortedList(childCoordinate, (a, b) => (DistanceBetween(b, currentDestinationRequest).CompareTo(DistanceBetween(a, currentDestinationRequest))));
                    }
                }
            }
        }

        static int DistanceBetween(Coordinate a, Coordinate b)
        {
            // Calculate "as the crow flies" distance
            int length = Mathf.Abs(a.x - b.x);
            int height = Mathf.Abs(a.y - b.y);
            int squaredHypotenouse = (length * length) + (height * height);
            return squaredHypotenouse;
        }

        static void BuildShortestPath(List<Coordinate> shortestPath, Coordinate destination, ref PathfindingNode[,] nodes)
        {
            Coordinate previousCoordinate = shortestPath[shortestPath.Count - 1];
            // Check if we have reached our destination
            if (previousCoordinate == destination)
            {
                return;
            }

            // Find which node to go to
            Coordinate nextNode = nodes[previousCoordinate.x, previousCoordinate.y].closestNodeToStart;
            shortestPath.Add(nextNode);
            BuildShortestPath(shortestPath, destination, ref nodes);
        }

        int GetGridDistanceFromWorldDistance(float worldDistance)
        {
            // Scale
            float scaledDistance = worldDistance / navData.settings.distanceBetweenCells;
            // Snap float value to grid value
            int gridDistance = (int)scaledDistance;
            // Round up
            gridDistance++;
            return gridDistance;
        }

        Coordinate GetGridPositionFromWorldPosition(Vector3 worldPosition)
        {
            // Convert worldPosition to an offset from our nav origin
            Vector3 worldOffset = worldPosition - navData.settings.origin;
            // Scale to our grid size
            Vector3 offsetFromGridCenter = worldOffset / navData.settings.distanceBetweenCells;
            // Round float values in grid space to closest grid value
            int xGridPosition = Mathf.RoundToInt(offsetFromGridCenter.x);
            // Use z coordinate instead of y because Unity is y-up
            int yGridPosition = Mathf.RoundToInt(offsetFromGridCenter.z);

            Coordinate retVal = new Coordinate(xGridPosition, yGridPosition);
            return retVal;
        }

        Vector3 GetWorldPositionFromGridPosition(int x, int y)
        {
            // Format
            Vector3 gridPositionVector3 = new Vector3(x, 0, y);
            // Scale up
            Vector3 worldScalePosition = gridPositionVector3 * navData.settings.distanceBetweenCells;
            // Offset
            Vector3 retVal = worldScalePosition + navData.settings.origin;

            return retVal;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!gizmoEnabled)
            {
                return;
            }

            DrawNavNodes();
        }

        void DrawNavNodes()
        {
            if (navData == null || navData.nodes == null)
            {
                return;
            }

            Camera sceneCamera = UnityEditor.SceneView.currentDrawingSceneView.camera;
            Vector3 sceneCameraPosition = sceneCamera.transform.position;
            
            Vector3 cubeScale = new Vector3(0.95f, 0.0f, 0.95f);
            if (gizmoCulling)
            {
                // Draw points only within a certain zone around the camera
                Coordinate cameraGridPosition = GetGridPositionFromWorldPosition(sceneCameraPosition);
                // Draw gizmos in a square around this position
                int width = GetGridDistanceFromWorldDistance(gizmoCullingDistance);
                int length = width;
                int startingGridPositionX = Mathf.Clamp(cameraGridPosition.x - (width / 2), 0, navData.settings.width);
                int startingGridPositionY = Mathf.Clamp(cameraGridPosition.y - (length / 2), 0, navData.settings.length);
                for (int x = startingGridPositionX; x < navData.settings.width && x < startingGridPositionX + width; x++)
                {
                    for (int y = startingGridPositionY;  y < navData.settings.length && y < startingGridPositionY + length; y++)
                    {
                        DrawPoint(x, y, cubeScale);
                    }
                }
            }
            else
            {
                for (int x = 0; x < navData.settings.width; x++)
                {
                    for (int y = 0; y < navData.settings.length; y++)
                    {
                        DrawPoint(x, y, cubeScale);
                    }
                }
            }
        }

        void DrawPoint(int x, int y, Vector3 gizmoScale)
        {
            Vector3 worldPosition = GetWorldPositionFromGridPosition(x, y);
            if (navData[x, y].isBlocked)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(worldPosition, gizmoScale * navData.settings.distanceBetweenCells);
            }
            else
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(worldPosition, gizmoScale * navData.settings.distanceBetweenCells);
            }
        }
    }
#endif
}