using LCY;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace STAR
{
    /// <summary>
    /// A quad that visualized user's field of view, since HoloLens's FOV is small..
    /// </summary>
    public class FOVQuad : MonoBehaviour
    {
        private void Start()
        {
            Configurations.Instance.SetAndAddCallback("Billboard_ShowFOVQuad", false, v => gameObject.SetActive(v), Configurations.CallNow.YES, Configurations.RunOnMainThead.YES);
            Configurations.Instance.AddCallback("*_PrepareUI", () => Configurations.Instance.Set("Billboard_ShowFOVQuad", false));
        }

        private void Update()
        {
        }
    }
}
