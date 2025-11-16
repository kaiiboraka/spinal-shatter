using System.Collections.Generic;
using Elythia;
using Godot;


public partial class ObjectPoolManager<T> : Node where T : Node, new()
{
    private DebugLogger DEBUG;

    [Export] public PackedScene Scene { get; set; }
    [Export] public int PoolMaxSize { get; set; } = 1000;

    public Node PoolParent { get; set; }
    private readonly Queue<T> _readyPool = new();
    private int _activeObjects = 0;

    public override void _EnterTree()
    {
        DEBUG = new DebugLogger(this);

        if (PoolParent == null)
        {
            PoolParent = new Node { Name = $"{typeof(T).Name}Pool_Default" };
            GetTree().Root.AddChild(PoolParent);
        }
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
            DEBUG.Info($"ObjectPoolManager: Attempted to release invalid or already released object: {obj?.Name ?? "null"}");
            return;
        }

        _activeObjects--;
        DEBUG.Info($"ObjectPoolManager: Releasing object {obj.Name}. Active objects: {_activeObjects}");

        obj.SetDeferred("visible", false);
        obj.SetPhysicsProcess(false);
        obj.SetProcess(false);
        if (obj.HasMethod("Reset"))
        {
	        obj.Call("Reset");
        }

        if (obj is Node3D node3D)
        {
            node3D.GlobalPosition = Vector3.Zero;
        }
        if (obj is Node2D node2D)
        {
            node2D.GlobalPosition = Vector2.Zero;
        }

        if (_readyPool.Count < PoolMaxSize)
        {
            _readyPool.Enqueue(obj);
            DEBUG.Info($"ObjectPoolManager: Enqueued {obj.Name}. Ready pool size: {_readyPool.Count}");
        }
        else
        {
            obj.QueueFree();
            DEBUG.Info($"ObjectPoolManager: Pool full, QueueFree() {obj.Name}.");
        }
    }

    public T Get()
    {
        if (_activeObjects >= PoolMaxSize)
        {
            DEBUG.Info($"ObjectPoolManager: Pool full, cannot get new object. Active objects: {_activeObjects}");
            return null;
        }

        T obj;

        if (_readyPool.Count > 0)
        {
            obj = _readyPool.Dequeue();
            DEBUG.Info($"ObjectPoolManager: Dequeued existing object {obj.Name}. Ready pool size: {_readyPool.Count}");
        }
        else
        {
            if (Scene == null)
            {
                GD.PrintErr($"ObjectPoolManager for {typeof(T).Name} has no scene packed!");
                return null;
            }
            obj = Scene.Instantiate<T>();
            PoolParent.AddChild(obj);
            DEBUG.Info($"ObjectPoolManager: Instantiated new object {obj.Name}.");
        }

        obj.SetProcess(true);
        obj.SetPhysicsProcess(true);
        obj.SetDeferred("visible", true);

        _activeObjects++;
        DEBUG.Info($"ObjectPoolManager: Got object {obj.Name}. Active objects: {_activeObjects}");

        return obj;
    }
}
