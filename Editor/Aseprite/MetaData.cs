using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Aseprite
{
    public enum MetadataType { UNKNOWN, TRANSFORM };

    public class Metadata
    {
        static public string MetadataChar = "@";

        public MetadataType Type { get; private set; } = default;
        public Dictionary<int, Vector2> Transforms { get; private set; } = default; // average position per frames
        public List<string> Args { get; private set; } = default;

        public Metadata(string layerName)
        {
            var regex = new Regex("@transform\\(\"(.*)\"\\)");
            var match = regex.Match(layerName);

            if (match.Success)
            {
                Type = MetadataType.TRANSFORM;
                Args = new List<string>();
                Args.Add(match.Groups[1].Value);
                Transforms = new Dictionary<int, Vector2>();
            }
            else
            {
                Debug.LogWarning($"Unsupported aseprite metadata {layerName}");
            }
        }
    }
}
