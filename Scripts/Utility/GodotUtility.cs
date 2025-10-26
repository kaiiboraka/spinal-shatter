using Godot;
using Godot.Collections;

public static class GodotUtility
{
    public static Dictionary<string, Resource> LoadResources(string path)
    {
        Dictionary<string, Resource> resources = new();

        DirAccess dirAccess = DirAccess.Open(path);
        if (dirAccess == null) { return null; }

        string[] files = dirAccess.GetFiles();
        if (files == null) { return null; }

        foreach(string fileName in files)
        {
            Resource loadedResource = GD.Load<Resource>(path + fileName);
            if (loadedResource == null) { continue; }

            resources[fileName] = loadedResource;
        }

        return resources;
    }

    public static void SaveResource(Resource resource, string path)
    {
        var saveResult = ResourceSaver.Save(resource, path);
        if (saveResult != Error.Ok)
        {
            GD.Print("could not save resource");
        }
    }
}
