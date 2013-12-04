using UnityEngine;
using System.Collections;

public class SetFocalDistance : MonoBehaviour {

    public Material DoFMaterial;
    public float currentDistance = 0;
    public float maxDistance = 100;

    void Update()
    {
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        var targetDistance = maxDistance;
        if (Physics.Raycast(ray, out hit))
            targetDistance = hit.distance;

        var delta = targetDistance - currentDistance;
        currentDistance += delta * Mathf.Clamp01(Time.deltaTime * 10);

        DoFMaterial.SetFloat("_FocalDistance", currentDistance);
    }
}
