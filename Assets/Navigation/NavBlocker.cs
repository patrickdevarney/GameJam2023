using UnityEngine;

public class NavBlocker : MonoBehaviour
{
    public bool isStatic = true;
    public Vector3 extents;
    public Vector3 Center => transform.position;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawCube(transform.position, extents * 2);
    }
}
