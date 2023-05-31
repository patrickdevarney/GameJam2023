using UnityEngine;
using UnityEditor;

namespace Navigation
{
    [CustomEditor(typeof(NavManager))]
    public class NavManagerInspector : Editor
    {
        public int testIterations = 1;
        NavManager targetManager;

        private void OnEnable()
        {
            targetManager = (NavManager)target;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if (GUILayout.Button("Bake!"))
            {
                NavBaker.Bake(targetManager.navData, targetManager.settings);
            }
            if (GUILayout.Button("Test random path"))
            {
                NavManagerTests.TestRandomPaths(targetManager, testIterations);
            }
            if (GUILayout.Button("Test lots of paths"))
            {
                NavManagerTests.TestRandomPaths(targetManager, testIterations);
            }
        }
    }
}