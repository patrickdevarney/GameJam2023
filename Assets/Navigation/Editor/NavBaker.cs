using UnityEngine;

namespace Navigation
{
    public class NavBaker
    {
        static int GetGridDistanceFromWorldDistance(float worldDistance, float distanceBetweenCells)
        {
            // Scale
            float scaledDistance = worldDistance / distanceBetweenCells;
            // Snap float value to grid value
            int gridDistance = (int)scaledDistance;
            // Round up
            gridDistance++;
            return gridDistance;
        }

        public static void Bake(BakedNavData navData, BakedNavData.BakeSettings settings)
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
            NavBlocker[] blockers = UnityEngine.Object.FindObjectsByType<NavBlocker>(FindObjectsSortMode.None);
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
                Coordinate lowerLeftGridPosition = navData.GetGridPositionFromWorldPosition(lowerLeftCorner);
                // Convert world length/width to grid length/width
                int blockerGridWidth = GetGridDistanceFromWorldDistance(blockers[i].extents.x * 2, settings.distanceBetweenCells);
                int blockerGridLength = GetGridDistanceFromWorldDistance(blockers[i].extents.z * 2, settings.distanceBetweenCells);
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
    }
}