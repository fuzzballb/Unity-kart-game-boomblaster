using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class WaterDecalSpawner : MonoBehaviour
{
    public float MinDuration = 0.5f;
    public float MaxDuration = 1;
    public float MinSize = 0.5f;
    public float MaxSize = 1;

    public Material DecalMaterial;
    public GameObject SpawnPlane;

    void Update()
    {
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        var plane = new Plane(SpawnPlane.transform.up, SpawnPlane.transform.position);
        float enter;
        if (Input.GetMouseButton(0) && plane.Raycast(ray, out enter))
        {
            SpawnDecal(ray.origin + ray.direction * enter);
        }
    }

    private void SpawnDecal(Vector3 position)
    {
        var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        plane.layer = SpawnPlane.layer;
        plane.renderer.sharedMaterial = (Material)Instantiate(DecalMaterial);
        //WaterHelper.CopyWaterProperties(SpawnPlane.renderer.sharedMaterial, plane.renderer.sharedMaterial);
        plane.transform.position = position;
        var decal = plane.AddComponent<WaterDecal>();
        DestroyImmediate(decal.collider);
        decal.Duration = Random.Range(MinDuration, MaxDuration);
        decal.MaxSize = Random.Range(MinSize, MaxSize);
    }
}
