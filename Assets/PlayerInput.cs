using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    public LayerMask navigationRaycastMask;
    public Camera raycastCamera;
    public Player selectedUnit;

    Vector3 previousHit;

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = raycastCamera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit, 100f, navigationRaycastMask))
            {
                selectedUnit.MoveTo(hit.point);
                previousHit = hit.point;
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(previousHit, 0.25f);
    }
}
