using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public float moveSpeed = 1f;
    List<Vector3> path;
    public float threshold = 0.1f;
    public float stepHeight = 0.25f;
    public LayerMask navigationBlockerMask;

    public bool useSmoothing = true;

    [Header("Gizmos")]
    public float gizmoRadius = 0.1f;
    public bool gizmoEnabled;

    private void Update()
    {
        if (path != null && path.Count > 0)
        {
            FollowPath();
        }
    }

    void FollowPath()
    {
        // Rigidly follow path
        transform.position = Vector3.MoveTowards(transform.position, path[0], moveSpeed * Time.deltaTime);

        if ((transform.position - path[0]).sqrMagnitude < threshold * threshold)
        {
            GetNextNode();
        }
    }

    void GetNextNode()
    {
        if (path.Count <= 1)
        {
            // Done
            path.Clear();
            return;
        }

        if (!useSmoothing)
        {
            path.RemoveAt(0);
            return;
        }

        // Per test requirements, we need to generate a stair-stepped path so we can't cull nodes in pathing but we could walk diagonals for visuals + smoothing for 45/90 turns
        Vector3 playerPosition = transform.position + Vector3.up * stepHeight;

        // We can at least reach index 1
        int reachableNodeIndex = 1;

        // Attempt to skip nodes by looking for shortcuts
        for (int i = 2; i < path.Count; i++)
        {
            Vector3 nodePosition = path[i] + Vector3.up * stepHeight;
            float rayDistance = (playerPosition - nodePosition).magnitude;
            if (Physics.Raycast(playerPosition, nodePosition - playerPosition, rayDistance, navigationBlockerMask))
            {
                // We hit something, we can't see this node
                break;
            }
            else
            {
                // We have clear line of sight of this node
                reachableNodeIndex = i;
            }
        }

        // Remove all nodes that are unnecessary
        path.RemoveRange(0, reachableNodeIndex);
    }

    public void MoveTo(Vector3 targetPosition)
    {
        path = Navigation.NavManager.Singleton.GetWorldPath(transform.position, targetPosition);
        if (path != null)
        {
            GetNextNode();
        }
    }

    private void OnDrawGizmos()
    {
        if (!gizmoEnabled)
        {
            return;
        }

        if (path != null)
        {
            Gizmos.color = Color.red;
            for (int i = 0; i < path.Count; i++)
            {
                Gizmos.DrawSphere(path[i], gizmoRadius);
            }
        }
    }
}
