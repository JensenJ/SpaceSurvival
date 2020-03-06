using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JUCL.Utilities {
    public static class JUCLPhysicsUtils
    {
        //Function to return the raycast hit data from the current mouse position, only on layers specified
        public static RaycastHit GetMousePositionRaycastData(Camera cam, LayerMask layerToCheck)
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            UnityEngine.Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, layerToCheck);
            return hit;
        }
    }
}
