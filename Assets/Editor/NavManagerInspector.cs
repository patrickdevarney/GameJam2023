using UnityEngine;
using UnityEditor;

namespace Navigation
{
    [CustomEditor(typeof(NavManager))]
    public class NavManagerInspector : Editor
    {
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
                targetManager.Bake();
            }
            if (GUILayout.Button("Test random path"))
            {
                targetManager.TestRandomPath();
            }
            if (GUILayout.Button("Test lots of paths"))
            {
                targetManager.StartTest();
            }
        }
    }
}