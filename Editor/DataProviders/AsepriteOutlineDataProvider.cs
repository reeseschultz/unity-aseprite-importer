using System.Collections.Generic;
using AsepriteImporter.Data;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace AsepriteImporter.DataProviders
{
    public class AsepriteOutlineDataProvider : ISpriteOutlineDataProvider
    {
        readonly AseFileImporter importer = default;

        public AsepriteOutlineDataProvider(AseFileImporter importer)
            => this.importer = importer;

        public List<Vector2[]> GetOutlines(GUID guid)
        {
            foreach (AseFileSpriteImportData data in importer.SpriteImportData)
                if (data.spriteID == guid.ToString())
                    return data.outline;

            return new List<Vector2[]>();
        }

        public float GetTessellationDetail(GUID guid)
        {
            for (var i = 0; i < importer.SpriteImportData.Length; ++i)
                if (importer.SpriteImportData[i].spriteID == guid.ToString())
                    return importer.SpriteImportData[i].tessellationDetail;

            return 0;
        }

        public void SetOutlines(GUID guid, List<Vector2[]> data)
        {
            for (var i = 0; i < importer.SpriteImportData.Length; ++i)
                if (importer.SpriteImportData[i].spriteID == guid.ToString())
                    importer.SpriteImportData[i].outline = data;
        }

        public void SetTessellationDetail(GUID guid, float value)
        {
            for (var i = 0; i < importer.SpriteImportData.Length; ++i)
                if (importer.SpriteImportData[i].spriteID == guid.ToString())
                    importer.SpriteImportData[i].tessellationDetail = value;
        }
    }
}
