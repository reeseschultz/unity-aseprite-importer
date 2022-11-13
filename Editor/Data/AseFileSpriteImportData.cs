using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_2020_2_OR_NEWER
using UnityEditor.AssetImporters;
#else
using UnityEditor.Experimental.AssetImporters;
#endif

namespace AsepriteImporter.Data
{
    [Serializable]
    public class AseFileSpriteImportData
    {
        public string name = default;

        //     Position and size of the Sprite in a given texture.
        public Rect rect = default;

        //     Pivot value represented by SpriteAlignment.
        public SpriteAlignment alignment = default;

        //     Pivot value represented in Vector2.
        public Vector2 pivot = default;

        //     Border value for the generated Sprite.
        public Vector4 border = default;

        //     Sprite Asset creation uses this outline when it generates the Mesh for the Sprite.
        //     If this is not given, SpriteImportData.tesselationDetail will be used to determine
        //     the mesh detail.
        public List<Vector2[]> outline = default;

        //     Controls mesh generation detail. This value will be ignored if SpriteImportData.ouline
        //     is provided.
        public float tessellationDetail = default;

        //     An identifier given to a Sprite. Use this to identify which data was used to
        //     generate that Sprite.
        public string spriteID = default;

        public SpriteImportData ToSpriteImportData()
            => new SpriteImportData()
            {
                alignment = alignment,
                border = border,
                name = name,
                outline = outline,
                pivot = pivot,
                rect = rect,
                spriteID = spriteID,
                tessellationDetail = tessellationDetail
            };
    }
}
