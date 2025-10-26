namespace Elythia;

using Godot;
using System.Collections.Generic;


public partial class ObjectPoolManager<T> : Node where T : Node, new()
{
    private DebugLogger DEBUG;

    [Export] public PackedScene Scene { get; set; }
    [Export] public int PoolMaxSize { get; set; } = 1000;

    private Node _poolParent;
    private readonly Queue<T> _readyPool = new();
    private int _activeObjects = 0;

    public override void _EnterTree()
    {
        DEBUG = new DebugLogger(this);

        _poolParent = new Node { Name = $"{typeof(T).Name}Pool" };
        GetTree().Root.CallDeferred("add_child", _poolParent);
    }

    public void ClearPool()
    {
        foreach (var obj in _readyPool)
        {
            if (IsInstanceValid(obj))
            {
                obj.QueueFree();
            }
        }
        _readyPool.Clear();
        _activeObjects = 0;
    }

    public void Release(T obj)
    {
        if (_activeObjects <= 0 || !IsInstanceValid(obj))
        {
            return;
        }

        _activeObjects--;

        obj.SetProcess(false);
        obj.SetPhysicsProcess(false);
        obj.SetDeferred("visible", false);
        
        if (obj.HasMethod("Reset"))
        {
	        obj.Call("Reset");
        }

        if (_readyPool.Count < PoolMaxSize)
        {
            _readyPool.Enqueue(obj);
        }
        else
        {
            obj.QueueFree();
        }
    }

    public T Get()
    {
        if (_activeObjects >= PoolMaxSize)
        {
            return null;
        }

        T obj;

        if (_readyPool.Count > 0)
        {
            obj = _readyPool.Dequeue();
        }
        else
        {
            if (Scene == null)
            {
                GD.PrintErr($"ObjectPoolManager for {typeof(T).Name} has no scene packed!");
                return null;
            }
            obj = Scene.Instantiate<T>();
            _poolParent.CallDeferred("add_child", obj);
        }

        obj.SetProcess(true);
        obj.SetPhysicsProcess(true);
        obj.SetDeferred("visible", true);

        _activeObjects++;

        return obj;
    }
}
