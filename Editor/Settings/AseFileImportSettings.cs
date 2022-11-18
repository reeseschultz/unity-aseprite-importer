using System;
using System.Collections.Generic;
using UnityEditor.Animations;
using UnityEngine;

namespace AsepriteImporter
{
    public enum AseFileImportType
    {
        Sprite,
        Tileset,
    }

    public enum TileNameType
    {
        Index,
        RowCol,
    }

    public enum EmptyTileBehaviour
    {
        Keep,
        Remove
    }

    public enum AseAnimationBindType
    {
        SpriteRenderer,
        UIImage
    }

    public enum AseAnimatorType
    {
        None,
        AnimatorController,
        AnimatorOverrideController
    }

    public enum TransparencyMode
    {
        Default,
        Mask
    }

    [Serializable]
    public class LayerMergeOptions
    {
        [SerializeField] public string Name = default;
        [SerializeField] public List<string> Layers = default;
    }

    [Serializable]
    public class AseFileImportSettings
    {
        [SerializeField] public AseFileImportType importType = AseFileImportType.Sprite;
        [SerializeField] public TransparencyMode transparencyMode = TransparencyMode.Default;
        [SerializeField] public Color transparentColor = Color.magenta;
        [SerializeField] public int pixelsPerUnit = 16;
        [SerializeField] public int spriteAlignment = 0;
        [SerializeField] public Vector2 spritePivot = new Vector2(0.5f, 0.5f);

        [SerializeField] public bool generateAnimations = true;
        [SerializeField] public bool createAnimationAssets = default;
        [SerializeField] public AseAnimationBindType bindType = AseAnimationBindType.SpriteRenderer;
        [SerializeField] public AseAnimatorType animType = AseAnimatorType.None;
        [SerializeField] public AnimatorController baseAnimator = default;
        [SerializeField] public bool buildAtlas = default;

        [SerializeField] public bool splitLayers = default;
        [SerializeField] public List<LayerMergeOptions> layerMergeOptions = default;

        [SerializeField] public Vector2Int tileSize = new Vector2Int(16, 16);
        [SerializeField] public TileNameType tileNameType = TileNameType.Index;
        [SerializeField] public EmptyTileBehaviour tileEmpty = EmptyTileBehaviour.Keep;

        [SerializeField] public string tagDelimiter = "@";
    }
}
