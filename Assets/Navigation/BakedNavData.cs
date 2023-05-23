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

        public bool IsWithinGrid(NavManager.Coordinate gridPosition)
        {
            return IsWithinGrid(gridPosition.x, gridPosition.y);
        }
    }
}