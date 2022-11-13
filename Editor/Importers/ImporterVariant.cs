using AsepriteImporter.Editors;

namespace AsepriteImporter.Importers
{
    public class ImporterVariant
    {
        public string Name { get; } = default;
        public SpriteImporter SpriteImporter { get; } = default;
        public SpriteImporter TileSetImporter { get; } = default;
        public SpriteImporterEditor Editor { get; } = default;

        public ImporterVariant(string name, SpriteImporter spriteImporter, SpriteImporter tileSetImporter, SpriteImporterEditor editor)
        {
            Name = name;
            SpriteImporter = spriteImporter;
            TileSetImporter = tileSetImporter;
            Editor = editor;
        }
    }
}
