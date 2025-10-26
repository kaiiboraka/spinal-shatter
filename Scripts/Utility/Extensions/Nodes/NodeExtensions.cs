using System;
using Godot;
using Godot.Collections;
using System.Collections.Generic;
using SpinalShatter;

public static class NodeExtensions
{
	public static void Clear(this Node node)
	{
		foreach (var child in node.GetChildren())
		{
			child.QueueFree();
		}
	}

	public static void QueueFreeAll(this IEnumerable<Node> objects)
	{
		foreach (var @object in objects)
		{
			@object.QueueFree();
		}
	}

	public static bool IsBehind(this Node2D self, Node2D target)
	{
		return target.FacingLeft() && self.GlobalPosition.IsRightOf(target.GlobalPosition) ||
			   target.FacingRight() && self.GlobalPosition.IsLeftOf(target.GlobalPosition);
	}

	public static Node FindByIid(this Node node, string iid)
	{
		return (Node)GD.Load<Script>("res://Scripts/Import/SceneGrabber.gd")
					   .Call("_get_entity_reference", iid);
	}

	public static bool IsReady(this Node node)
	{
		return node.IsNodeReady() && node.IsInsideTree() && node.GetParent() != null;
	}

	public static bool IsInGame(this Node node)
	{
		return node.IsReady() && !Engine.IsEditorHint();
	}

	public static bool StopInEditor(this Node node, bool includeProcess = true)
	{
		bool playing = !Engine.IsEditorHint();
		if (includeProcess)
		{
			node.SetProcess(playing);
			node.SetPhysicsProcess(playing);
		}

		return !playing;
	}

	public static void SafeKill(this Tween tween)
	{
		if (tween.IsValid() && tween.IsRunning()) tween.Kill();
	}

	public static void SafeStop(this Tween tween)
	{
		if (tween.IsValid() && tween.IsRunning()) tween.Stop();
	}

	public static void StartNoReset(this Timer timer)
	{
		if (timer.IsStopped()) timer.Start();
	}

	public static bool SafeSubscribe(this GodotObject eventOwner, StringName signal, Action callback)
	{
		var callable = Callable.From(callback);
		if (eventOwner.IsConnected(signal, callable)) return false;

		eventOwner.Connect(signal, callable);

		// GD.Print($"{combatant.Name}.{Name}: TrySubscribe {nameof(OnCurrentStatsUpdated)} to {UnitStats.SignalName.StatsUpdated}: {connected}");
		return eventOwner.IsConnected(signal, callable);
	}

	public static bool SafeSubscribe<T>(this GodotObject eventOwner, StringName signal, Action<T> callback)
	{
		var callable = Callable.From(callback);
		if (eventOwner.IsConnected(signal, callable)) return false;

		eventOwner.Connect(signal, callable);
		return eventOwner.IsConnected(signal, callable);
	}

	public static bool SafeSubscribe<T1, T2>(this GodotObject eventOwner, StringName signal, Action<T1, T2> callback)
	{
		var callable = Callable.From(callback);
		if (eventOwner.IsConnected(signal, callable)) return false;

		eventOwner.Connect(signal, callable);
		return eventOwner.IsConnected(signal, callable);
	}

	public static bool SafeSubscribe<T1, T2, T3>(this GodotObject eventOwner, StringName signal,
												 Action<T1, T2, T3> callback)
	{
		var callable = Callable.From(callback);
		if (eventOwner.IsConnected(signal, callable)) return false;

		eventOwner.Connect(signal, callable);
		return eventOwner.IsConnected(signal, callable);
	}

	public static bool SafeSubscribe<T1, T2, T3, T4>(this GodotObject eventOwner, StringName signal,
													 Action<T1, T2, T3, T4> callback)
	{
		var callable = Callable.From(callback);
		if (eventOwner.IsConnected(signal, callable)) return false;

		eventOwner.Connect(signal, callable);
		return eventOwner.IsConnected(signal, callable);
	}


	public static bool SafeUnsubscribe(this GodotObject eventOwner, StringName signal, Action callback)
	{
		var callable = Callable.From(callback);
		if (!eventOwner.IsConnected(signal, callable)) return false;

		eventOwner.Disconnect(signal, callable);
		return eventOwner.IsConnected(signal, callable);
	}

	public static bool SafeUnsubscribe<T>(this GodotObject eventOwner, StringName signal, Action<T> callback)
	{
		var callable = Callable.From(callback);
		if (!eventOwner.IsConnected(signal, callable)) return false;

		eventOwner.Disconnect(signal, callable);
		return eventOwner.IsConnected(signal, callable);
	}

	public static bool SafeUnsubscribe<T1, T2>(this GodotObject eventOwner, StringName signal, Action<T1, T2> callback)
	{
		var callable = Callable.From(callback);
		if (!eventOwner.IsConnected(signal, callable)) return false;

		eventOwner.Disconnect(signal, callable);
		return eventOwner.IsConnected(signal, callable);
	}

	public static bool SafeUnsubscribe<T1, T2, T3>(this GodotObject eventOwner, StringName signal,
												   Action<T1, T2, T3> callback)
	{
		var callable = Callable.From(callback);
		if (!eventOwner.IsConnected(signal, callable)) return false;

		eventOwner.Disconnect(signal, callable);
		return eventOwner.IsConnected(signal, callable);
	}

	public static bool SafeUnsubscribe<T1, T2, T3, T4>(this GodotObject eventOwner, StringName signal,
													   Action<T1, T2, T3, T4> callback)
	{
		var callable = Callable.From(callback);
		if (!eventOwner.IsConnected(signal, callable)) return false;

		eventOwner.Disconnect(signal, callable);
		return eventOwner.IsConnected(signal, callable);
	}

	public static bool IsCurrentScene(this Node node)
	{
		return node.GetTree().CurrentScene.SceneFilePath == node.SceneFilePath;
	}

	public static Array<Node> GetAllDescendants(this Node node)
	{
		var descendants = new Array<Node>();
		foreach (var child in node.GetChildren())
		{
			descendants.AddRange(child.GetAllDescendants());
			descendants.Add(child);
		}

		return descendants;
	}

	public static Array<Node> GetSiblings(this Node node)
	{
		return node.GetParent().GetChildren();
	}

	public static void AddCollisionLayer2D(this CollisionObject2D node, SpinalShatter.LayerNames.PHYSICS_2D layer)
	{
		Callable.From(()=>node.SetCollisionLayerValue((int)layer, true)).CallDeferred();
	}

	public static void RemoveCollisionLayer2D(this CollisionObject2D node, SpinalShatter.LayerNames.PHYSICS_2D layer)
	{
		Callable.From(()=>node.SetCollisionLayerValue((int)layer, false)).CallDeferred();
	}

	public static void ToggleCollisionLayer2D(this CollisionObject2D node, SpinalShatter.LayerNames.PHYSICS_2D layer)
	{
		bool oldValue = node.GetCollisionLayerValue((int)layer);
		node.SetCollisionLayerValue((int)layer, !oldValue);
	}


	public static void AddCollisionLayer3D(this CollisionObject3D node, SpinalShatter.LayerNames.PHYSICS_3D layer)
	{
		node.SetCollisionLayerValue((int)layer, true);
	}

	public static void RemoveCollisionLayer3D(this CollisionObject3D node, SpinalShatter.LayerNames.PHYSICS_3D layer)
	{
		node.SetCollisionLayerValue((int)layer, false);
	}

	public static void ToggleCollisionLayer3D(this CollisionObject3D node, SpinalShatter.LayerNames.PHYSICS_3D layer)
	{
		bool oldValue = node.GetCollisionLayerValue((int)layer);
		node.SetCollisionLayerValue((int)layer, !oldValue);
	}

	public static void AddCollisionMask2D(this CollisionObject2D node, SpinalShatter.LayerNames.PHYSICS_2D mask)
	{
		node.SetCollisionMaskValue((int)mask, true);
	}

	public static void RemoveCollisionMask2D(this CollisionObject2D node, SpinalShatter.LayerNames.PHYSICS_2D mask)
	{
		node.SetCollisionMaskValue((int)mask, false);
	}

	public static void ToggleCollisionMask2D(this CollisionObject2D node, SpinalShatter.LayerNames.PHYSICS_2D mask)
	{
		bool oldValue = node.GetCollisionMaskValue((int)mask);
		node.SetCollisionMaskValue((int)mask, !oldValue);
	}

	public static void AddCollisionMask3D(this CollisionObject3D node, SpinalShatter.LayerNames.PHYSICS_3D mask)
	{
		node.SetCollisionMaskValue((int)mask, true);
	}

	public static void RemoveCollisionMask3D(this CollisionObject3D node, SpinalShatter.LayerNames.PHYSICS_3D mask)
	{
		node.SetCollisionMaskValue((int)mask, false);
	}

	public static void ToggleCollisionMask3D(this CollisionObject3D node, SpinalShatter.LayerNames.PHYSICS_3D mask)
	{
		bool oldValue = node.GetCollisionMaskValue((int)mask);
		node.SetCollisionMaskValue((int)mask, !oldValue);
	}
}