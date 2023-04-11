using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class CutoutMaskUI : Image
{
    public override Material materialForRendering
    {
        get
        {
            Material material = Instantiate(base.materialForRendering);
            material.SetInt("_StencilComp", (int)CompareFunction.NotEqual);
            return material;
        }
    }
}