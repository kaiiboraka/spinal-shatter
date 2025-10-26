using System.Collections;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

public static class ContainerExtensions
{
    public static bool IsNullOrEmpty(this Array array)
    {
        return array == null || array.Count == 0;
    }

    public static bool IsNullOrEmpty(this ArrayList arrayList)
    {
        return arrayList == null || arrayList.Count == 0;
    }
    
    public static bool IsNullOrEmpty(this Dictionary dictionary)
    {
        return dictionary == null || dictionary.Count == 0;
    }
    
    public static bool IsNullOrEmpty<T>(this HashSet<T> set)
    {
        return set == null || set.Count == 0;
    }
    
    public static bool IsNullOrEmpty<[MustBeVariant]T>(this Array<T> array)
    {
        return array == null || array.Count == 0;
    }

    public static void Insert<T>(this Queue<T> queue, int index, T item)
    {
        var array = new List<T>(queue.ToArray());
        array.Insert(index, item);
        queue = new Queue<T>(array);
        queue.Clear();
        foreach (T item1 in array)
        {
            queue.Enqueue(item1);
        }
    }

    public static void PushFront<[MustBeVariant]T>(this Array<T> arr, T item)
    {
        arr.Insert(0, item);
    }

    public static void PushFront<T>(this List<T> list, T item)
    {
        list.Insert(0, item);
    }

    public static void PushFront(this ArrayList arrayList, object item)
    {
        arrayList.Insert(0, item);
    }

    public static void PushFront<T>(this Queue<T> queue, T item)
    {
        var array = new List<T>(queue.ToArray());
        array.Insert(0, item);
        queue.Clear();
        foreach (T queueItem in array)
        {
            queue.Enqueue(queueItem);
        }
    }

    public static void PushBack<[MustBeVariant]T>(this Array<T> arr, T item)
    {
        arr.Add(item);
    }

    public static void PushBack<T>(this List<T> list, T item)
    {
        list.Add(item);
    }

    public static void PushBack(this ArrayList arrayList, object item)
    {
        arrayList.Add(item);
    }

    public static void PushBack<T>(this Queue<T> queue, T item)
    {
        queue.Enqueue(item);
    }


    public static T PopFront<[MustBeVariant]T>(this Array<T> arr)
    {
        var first = arr[0];
        arr.RemoveAt(0);
        return first;
    }

    public static T PopFront<T>(this List<T> arr)
    {
        var first = arr[0];
        arr.RemoveAt(0);
        return first;
    }

    public static object PopFront(this ArrayList arr)
    {
        var first = arr[0];
        arr.RemoveAt(0);
        return first;
    }

    public static T PopFront<T>(this Queue<T> queue)
    {
        return queue.Dequeue();
    }


    public static T PopBack<[MustBeVariant]T>(this Array<T> arr)
    {
        var last = arr[^1];
        arr.RemoveAt(arr.Count-1);
        return last;
    }

    public static T PopBack<T>(this List<T> arr)
    {
        var last = arr[^1];
        arr.RemoveAt(arr.Count-1);
        return last;
    }

    public static object PopBack(this ArrayList arr)
    {
        var last = arr[^1];
        arr.RemoveAt(arr.Count-1);
        return last;
    }

    public static T PopBack<T>(this Queue<T> queue)
    {
        var array = new List<T>(queue.ToArray());
        var last = array[^1];
        array.RemoveAt(array.Count - 1);
        queue.Clear();
        foreach (T item in array)
        {
            queue.Enqueue(item);
        }
        return last;
    }

    public static bool AddUnique<[MustBeVariant]T>(this Array<T> arr, T item)
    {
        if (arr.Contains(item)) return false;
        arr.Add(item);
        return true;
    }

    public static bool AddUnique<T>(this List<T> list, T item)
    {
        if (list.Contains(item)) return false;
        list.Add(item);
        return true;
    }

    public static bool AddUnique(this ArrayList arrayList, object item)
    {
        if (arrayList.Contains(item)) return false;
        arrayList.Add(item);
        return true;
    }

    public static bool AddUnique<T>(this HashSet<T> set, T item)
    {
        return set.Add(item);
    }

    public static bool AddUnique<T>(this Queue<T> queue, T item)
    {
        if (queue.Contains(item)) return false;
        queue.Enqueue(item);
        return true;
    }

    public static bool Remove<T>(this Queue<T> queue, T item)
    {
        var array = new List<T>(queue.ToArray());
        bool removed = array.Remove(item);
        if (removed)
        {
            queue.Clear();
            foreach (T queueItem in array)
            {
                queue.Enqueue(queueItem);
            }
        }
        return removed;
    }

    public static Array<Node> GetAllChildren(this Node root, bool includeInternal=false)
    {
        var children = root.GetChildren(includeInternal);
        var results = children;
        foreach (var n in children)
        {
            results.AddRange(n.GetAllChildren(includeInternal));
        }
        return results;
    }
}