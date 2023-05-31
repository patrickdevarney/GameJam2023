using UnityEngine;

namespace Navigation
{
    [CreateAssetMenu(fileName = "NavData", menuName = "ScriptableObject/NavData")]
    public class BakedNavData : ScriptableObject
    {
        [System.Serializable]
        // Stores data that is unique from node-to-node
        public struct NavNode
        {
            public bool isBlocked;
        }

        [System.Serializable]
        // All settings that affect the baked data
        public struct BakeSettings
        {
            public int width;
            public int length;
            public float distanceBetweenCells;
            public Vector3 origin;

            public static bool operator ==(BakeSettings a, BakeSettings b)
            {
                return (a.width == b.width &&
                    a.length == b.length &&
                    a.distanceBetweenCells == b.distanceBetweenCells &&
                    a.origin == b.origin);
            }

            public static bool operator !=(BakeSettings a, BakeSettings b)
            {
                return !(a == b);
            }
        }

        [HideInInspector]
        public NavNode[] nodes;

        public BakeSettings settings;

        public ref NavNode this[int x, int y] => ref nodes[(y * settings.length) + x];

        public bool IsWithinGrid(int x, int y)
        {
            if (x >= 0 &&
                x < settings.width &&
                y >= 0 &&
                y < settings.length)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool IsWithinGrid(Coordinate gridPosition)
        {
            return IsWithinGrid(gridPosition.x, gridPosition.y);
        }

        public Coordinate GetGridPositionFromWorldPosition(Vector3 worldPosition)
        {
            // Convert worldPosition to an offset from our nav origin
            Vector3 worldOffset = worldPosition - settings.origin;
            // Scale to our grid size
            Vector3 offsetFromGridCenter = worldOffset / settings.distanceBetweenCells;
            // Round float values in grid space to closest grid value
            int xGridPosition = Mathf.RoundToInt(offsetFromGridCenter.x);
            // Use z coordinate instead of y because Unity is y-up
            int yGridPosition = Mathf.RoundToInt(offsetFromGridCenter.z);

            Coordinate retVal = new Coordinate(xGridPosition, yGridPosition);
            return retVal;
        }

        public Vector3 GetWorldPositionFromGridPosition(int x, int y)
        {
            // Format
            Vector3 gridPositionVector3 = new Vector3(x, 0, y);
            // Scale up
            Vector3 worldScalePosition = gridPositionVector3 * settings.distanceBetweenCells;
            // Offset
            Vector3 retVal = worldScalePosition + settings.origin;

            return retVal;
        }

        public int GetGridDistanceFromWorldDistance(float worldDistance)
        {
            // Scale
            float scaledDistance = worldDistance / settings.distanceBetweenCells;
            // Snap float value to grid value
            int gridDistance = (int)scaledDistance;
            // Round up
            gridDistance++;
            return gridDistance;
        }
    }
}