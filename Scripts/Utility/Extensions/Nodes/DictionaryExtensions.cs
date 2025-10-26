namespace Elythia;

using Godot;
using Godot.Collections;

public static class DictionaryExtensions
{

	#region Keys And Values

	public static Dictionary<TOuter, Dictionary<string, string>> StringifyNestedKeysValues
		<[MustBeVariant] TOuter, [MustBeVariant] TInner, [MustBeVariant] TValue>
		(this Dictionary<TOuter, Dictionary<TInner, TValue>> source)
	{
		return StringifyNestedDictionaryKeysValues(source);
	}
	public static Dictionary<TOuter, Dictionary<string, string>> StringifyNestedDictionaryKeysValues
		<[MustBeVariant] TOuter, [MustBeVariant] TInner, [MustBeVariant] TValue>
		(Dictionary<TOuter, Dictionary<TInner, TValue>> source)
	{
		if (source == null) return null;
		var result = new Dictionary<TOuter, Dictionary<string, string>>();
		foreach (var outerKey in source.Keys)
		{
			result.Add(outerKey, StringifyDictionaryKeysValues(source[outerKey]));
		}
		return result;
	}
	public static Dictionary<string, string> StringifyKeysValues
		<[MustBeVariant] TKey, [MustBeVariant] TValue>
		(this Dictionary<TKey, TValue> source)
	{
		return StringifyDictionaryKeysValues(source);
	}
	public static Dictionary<string, string> StringifyDictionaryKeysValues
		<[MustBeVariant] TKey, [MustBeVariant] TValue>
		(Dictionary<TKey, TValue> source)
	{
		if (source == null) return null;
		var result = new Dictionary<string, string>();
		foreach (var key in source.Keys)
		{
			result.Add(key.ToString() ?? "NULL KEY", source[key].ToString() ?? "NULL value");
		}
		return result;
	}

	#endregion Keys And Values

	#region Just Keys




	public static Dictionary<TOuter, Dictionary<string, TValue>> StringifyNestedKeys
		<[MustBeVariant] TOuter, [MustBeVariant] TInner, [MustBeVariant] TValue>
		(this Dictionary<TOuter, Dictionary<TInner, TValue>> source)
	{
		return StringifyNestedDictionaryKeys(source);
	}

	public static Dictionary<TOuter, Dictionary<string, TValue>> StringifyNestedDictionaryKeys
		<[MustBeVariant] TOuter, [MustBeVariant] TInner, [MustBeVariant] TValue>
		(Dictionary<TOuter, Dictionary<TInner, TValue>> source)
	{
		if (source == null) return null;
		var result = new Dictionary<TOuter, Dictionary<string, TValue>>();
		foreach (var outerKey in source.Keys)
		{
			result.Add(outerKey, StringifyDictionaryKeys(source[outerKey]));
		}
		return result;
	}

	public static Dictionary<string, TValue> StringifyKeys
		<[MustBeVariant] TKey, [MustBeVariant] TValue>
		(this Dictionary<TKey, TValue> source)
	{
		return StringifyDictionaryKeys(source);
	}
	public static Dictionary<string, TValue> StringifyDictionaryKeys
		<[MustBeVariant] TKey, [MustBeVariant] TValue>
		(Dictionary<TKey, TValue> source)
	{
		if (source == null) return null;
		var result = new Dictionary<string, TValue>();
		foreach (var key in source.Keys)
		{
			result.Add(key.ToString() ?? "NULL KEY", source[key]);
		}
		return result;
	}

	#endregion  Just Keys

	#region Just Values

	public static Dictionary<TOuter, Dictionary<TInner, string>> StringifyNestedValues
		<[MustBeVariant] TOuter, [MustBeVariant] TInner, [MustBeVariant] TValue>
		(this Dictionary<TOuter, Dictionary<TInner, TValue>> source)
	{
		return StringifyNestedDictionaryValues(source);
	}

	public static Dictionary<TOuter, Dictionary<TInner, string>> StringifyNestedDictionaryValues
		<[MustBeVariant] TOuter, [MustBeVariant] TInner, [MustBeVariant] TValue>
		(Dictionary<TOuter, Dictionary<TInner, TValue>> source)
	{
		if (source == null) return null;
		var result = new Dictionary<TOuter, Dictionary<TInner, string>>();
		foreach (var outerKey in source.Keys)
		{
			result.Add(outerKey, StringifyDictionaryValues(source[outerKey]));
		}
		return result;
	}

	public static Dictionary<TKey, string> StringifyValues
		<[MustBeVariant] TKey, [MustBeVariant] TValue>
		(this Dictionary<TKey, TValue> source)
	{
		return StringifyDictionaryValues(source);
	}
	public static Dictionary<TKey, string> StringifyDictionaryValues
		<[MustBeVariant] TKey, [MustBeVariant] TValue>
		(Dictionary<TKey, TValue> source)
	{
		if (source == null) return null;
		var result = new Dictionary<TKey, string>();
		foreach (var key in source.Keys)
		{
			result.Add(key, source[key].ToString() ?? "NULL KEY");
		}
		return result;
	}

	#endregion  Just Values

}