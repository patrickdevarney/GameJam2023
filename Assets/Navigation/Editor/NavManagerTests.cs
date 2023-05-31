using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Navigation
{
    public class NavManagerTests
    {
        public static void TestRandomPaths(NavManager navManager, int iterations)
        {
            long totalElapsedMilliseconds = 0;
            for (int i = 0; i < iterations; i++)
            {
                long elapsedMilliseconds = TestRandomPath(navManager);
                totalElapsedMilliseconds += elapsedMilliseconds;
            }
            Debug.Log($"Ran {iterations} tests. Average test {totalElapsedMilliseconds / iterations}ms");
        }

        static long TestRandomPath(NavManager navManager)
        {
            Coordinate from = new Coordinate
            {
                x = Random.Range(0, navManager.navData.settings.width),
                y = Random.Range(0, navManager.navData.settings.length),
            };

            Coordinate to = new Coordinate
            {
                x = Random.Range(0, navManager.navData.settings.width),
                y = Random.Range(0, navManager.navData.settings.length),
            };

            Vector3 worldFrom = navManager.navData.GetWorldPositionFromGridPosition(from.x, from.y);
            Vector3 worldTo = navManager.navData.GetWorldPositionFromGridPosition(to.x, to.y);

            var timer = new System.Diagnostics.Stopwatch();
            timer.Start();
            navManager.GetWorldPath(worldFrom, worldTo);
            timer.Stop();
            Debug.Log($"Time elapsed: {timer.ElapsedMilliseconds}ms");
            return timer.ElapsedMilliseconds;
        }
    }
}