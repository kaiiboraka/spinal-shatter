namespace Elythia;

using System;
using Godot;
using Godot.Collections;

public static class FileSystem
{
	public static void ForFilesInDirectory(string path, Action<string, string> fileAction,
										   bool includeSubdirectories = false)
	{
		using var dir = DirAccess.Open(path);
		dir.ListDirBegin();

		for (string filename = dir.GetNext();; filename = dir.GetNext())
		{
			if (filename.IsNullOrEmpty()) break;

			if (!dir.CurrentIsDir())
			{
				fileAction(filename, $"{path}/{filename}");
			}

			if (dir.CurrentIsDir() && includeSubdirectories)
			{
				ForFilesInDirectory($"{path}/{filename}", fileAction, true);
			}
		}

		dir.ListDirEnd();
	}

	/// <summary>
	///
	/// </summary>
	/// <param name="path">Exclude open slash, include close slash. E.g.: "Items/Bubbles/".</param>
	/// <param name="subType"></param>
	/// <typeparam name="T"></typeparam>
	/// <returns></returns>
	public static Array<T> LoadAllResourcesOfType<[MustBeVariant] T>(string path, string subType) where T : Resource
	{
		if (path[0] == '/') path = path.Substring(1);
		if (path[^1] != '/') path += '/';

		Array<Resource> resources = new();
		resources = resources.LoadAll($"res://Assets/CustomResources/{path}{subType}");
		var list = new Array<T>();
		foreach (var resource in resources)
		{
			if (resource is not T dataType) continue;

			if (dataType.ResourceName.Contains($"{subType}") ||
				dataType.ResourcePath.FileName().Contains($"{subType}"))
			{
				list.Add(dataType);
			}
		}

		return list;
	}

	public static Array<T> LoadAllResourcesOfType<[MustBeVariant] T>(string path) where T : Resource
	{
		if (path[0] == '/') path = path.Substring(1);
		if (path[^1] != '/') path += '/';

		Array<Resource> resources = new();
		resources = resources.LoadAll($"res://Assets/CustomResources/{path}");
		var list = new Array<T>();
		foreach (var resource in resources)
		{
			if (resource is T dataType)
			{
				list.Add(dataType);
			}
		}

		return list;
	}

	public static Array<Resource> LoadAllResources(string path)
	{
		if (path[0] == '/') path = path.Substring(1);
		if (path[^1] != '/') path += '/';

		Array<Resource> resources = new();
		return resources.LoadAll($"res://Assets/CustomResources/{path}");
	}

	/// <summary>
	///
	/// </summary>
	/// <param name="path">Exclude open slash, include close slash. E.g.: "Items/Bubbles/".</param>
	/// <param name="subType"></param>
	/// <typeparam name="T"></typeparam>
	/// <returns></returns>
	public static Array<T> LoadAllPackedScenesOfType<[MustBeVariant] T>(string path, string subType)
		where T : PackedScene
	{
		Array<PackedScene> resources = new();
		resources = resources.LoadAll($"res://Scenes/{path}{subType}/");
		var list = new Array<T>();
		foreach (var resource in resources)
		{
			if (resource is T container && container.ResourceName.Contains($"{subType}_"))
			{
				list.Add(container);
			}
		}

		return list;
	}
}