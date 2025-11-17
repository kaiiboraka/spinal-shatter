using System;
// using Limbo.Console.Sharp;

namespace Elythia;

using Godot;
using static Godot.GD;

[GlobalClass, Tool]
public partial class DebugLogger : RefCounted
{
	private static char[] DELIMITERS = { '[', ']' };

	private string objectName;
	private string typeName;
	private string className;

	public DebugLogger()
	{}

	public DebugLogger(Node loggee)
	{
		objectName = loggee.Name;
		className = loggee.GetClass();
		typeName = loggee.GetType().Name;
	}

	public DebugLogger(Node parent, Node self)
	{
		objectName = self.Name;
		typeName = self.GetType().Name;
		className = parent.Name;
	}

	private static string GetLevelName(LogLevel level)
	{
		return level switch
		{
			LogLevel.TRACE => "TRACE",
			LogLevel.DEBUG => "DEBUG",
			LogLevel.INFO => "INFO",
			LogLevel.WARNING => "WARNING",
			LogLevel.ERROR => "ERROR",
			_ => "UNKNOWN"
		};
	}

	private static string GetLevelColorHex(LogLevel level)
	{
		return GetLevelColor(level).ToHex();
	}

	private static Color GetLevelColor(LogLevel level)
	{
		return level switch
		{
			LogLevel.TRACE => Colors.Cyan,
			LogLevel.DEBUG => Colors.LightGreen,
			LogLevel.INFO => Colors.LightSkyBlue,
			LogLevel.WARNING => Colors.Gold,
			LogLevel.ERROR => Colors.Red,
			_ => Colors.White
		};
	}

	private string GetHeader(LogLevel level)
	{
		var level_name = GetLevelName(level);
		float time = Time.GetTicksMsec();
		return $"{DELIMITERS[0]}[color={GetLevelColorHex(level)}]{level_name}[/color] : {time.MsToSec()}{DELIMITERS[1]}" +
			   $"{DELIMITERS[0]}{objectName} @ {typeName}: {className}{DELIMITERS[1]}";
	}

	private void Log(LogLevel level, string message, Color printColor = default)
	{
		string text = "";
		string color = printColor == default ? GetLevelColorHex(level)
			: printColor.ToHex();
		text = $"[color={color}]{message}[/color]";
		var headerText = $"{GetHeader(level)} {text}";
		PrintRich(headerText);

		if (Engine.IsEditorHint()) return;
		switch (level)
		{
			case LogLevel.TRACE:
				// LimboConsole.PrintLine(text);
				break;
			case LogLevel.DEBUG:
				// LimboConsole.Debug(text);
				break;
			case LogLevel.INFO:
				// LimboConsole.Info(text);
				break;
			case LogLevel.WARNING:
				PushWarning(message);
				// LimboConsole.Warn(text);
				break;
			case LogLevel.ERROR:
				PushError(message);
				// LimboConsole.Error(text);
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(level), level, null);
		}
	}

	public void Trace(string message)
	{
		Log(LogLevel.TRACE, message);
	}

	public void Debug(string message)
	{
		Log(LogLevel.DEBUG, message);
	}

	public void Info(string message)
	{
		Log(LogLevel.INFO, message);
	}

	public void Warning(string message)
	{
		Log(LogLevel.WARNING, message);
	}

	public void Error(string message)
	{
		Log(LogLevel.ERROR, message);
	}
}
