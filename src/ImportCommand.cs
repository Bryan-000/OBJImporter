namespace OBJImporter;

using GameConsole;

public class ImportCommand : ICommand
{
    public string Name => "Import";
    public string Description => "Imports an OBJ file into ultrakill.";
    public string Command => "import";

    public void Execute(Console con, string[] args)
    {
        string path = string.Join(' ', args).Trim('"');

        Importer.CreateGameObject(path, NewMovement.Instance?.transform.position);
    }
}