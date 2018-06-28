using HoloToolkit.Unity.SpatialMapping;
using LCY;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomManager : MonoBehaviour
{
	private void Start()
    {
        SurfaceMeshesToPlanes.Instance.MakePlanesComplete += MakePlanesComplete;
        Configurations.Instance.SetAndAddCallback("SpatialMap_MakePlane", false, v => SurfaceMeshesToPlanes.Instance.MakePlanes(), Configurations.RunOnMainThead.YES);
    }

    protected void MakePlanesComplete(object source, System.EventArgs args)
    {
        List<GameObject> tables = new List<GameObject>();
        tables = SurfaceMeshesToPlanes.Instance.GetActivePlanes(PlaneTypes.Wall | PlaneTypes.Floor | PlaneTypes.Table | PlaneTypes.Ceiling | PlaneTypes.Unknown);
        Debug.Log("wbewbweb" + tables.Count);

        RemoveSurfaceVertices.Instance.RemoveSurfaceVerticesWithinBounds(SurfaceMeshesToPlanes.Instance.ActivePlanes);
    }
}
