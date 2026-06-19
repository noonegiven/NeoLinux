namespace NeoLinux;
using System;
using System.Text;
using System.Linq;

public static class commands
{
    private static string run { get; set; }= "cmd:";
    public static string arg { get; set; } = "--";
    private static string path = Path.Combine(AppContext.BaseDirectory, "cache", "cacheCommand");
    private static string cmdCache { get; set; } = Path.Combine(path, "cacheCommand.sysNeo");
    
    private static string[] CommandSplit { get; set; }
    private static string[] Dependencies { get; set; }
    private static string[] cmd { get; set; } =
    [
        "exit", "open", "clear", "help",
        "apps", "install",
        "ls", "cd", "pwd", "rm", "ff",
        "rf", "rd", "fm"
    ];
    private static string[] apps { get; set; } =
    [
        "noise", "chronos", "hermes",
        "mars", "espresso", "plume",
        "neofetch", "connect"
    ];
    private static string[] cmdHelp { get; set; } =
    [
        "---- === COMMANDS ===----",
        "unknown -> wrong command",
        "exit -> exits the system",
        "open -> opens an app",
        "clear -> clears the console",
        "help -> shows this message",
        "ls -> lists files in the current directory",
        "cd -> changes the current directory",
        "pwd -> shows the current directory",
        "rm -> create a file or directory",
        "ff -> find a file or directory",
        "rf -> run a file (you must specify a file with '--'",
        "rd -> read a file",
        "fm -> delete a file",
        "---- === APPS ===----",
        "install -> installs or repairs NeoOS system files",
        "noise -> text editor / IDE",
        "chronos -> text generating AI",
        "hermes -> messaging",
        "mars -> ?creative type thing",
        "plume -> note taking manager",
        "espresso -> to do list manager",
    ];


    public static bool CheckCommand(string? content)
    {
        if (!string.IsNullOrWhiteSpace(content) && content.StartsWith(run)) return true;
        else return false;
    }

    public static (string command, string argument, string[] dependency) HandleCommand(string content)
    {
        CommandSplit = content.Split(" ", StringSplitOptions.RemoveEmptyEntries);
        if (CommandSplit.Length < 2 || CommandSplit[0] != run)
            { return ("unknown", "", Array.Empty<string>()); }
        if (CommandSplit[1] == "help")
        { Console.WriteLine("Available commands:");
            foreach (var c in cmdHelp)
            { Console.WriteLine(c); }
            return ("help", "", Array.Empty<string>()); }
        if (CommandSplit[1] == "apps")
        { Console.WriteLine("installed apps:");
            foreach (var c in apps) { Console.WriteLine(c); }
            return ("apps", "", Array.Empty<string>()); }
        if (CommandSplit[1] == "clear")
            { Console.Clear(); return ("clear", "", Array.Empty<string>()); }
        if (!cmd.Contains(CommandSplit[1]))
            { return ("unknown", "", Array.Empty<string>()); }
        if (CommandSplit.Length == 2) return (CommandSplit[1], "", Array.Empty<string>());
        if (CommandSplit.Length > 3) {
            content = content.Replace($"{run} {CommandSplit[1]} {CommandSplit[2]}", ""); }
        Dependencies = CommandSplit.Skip(3).ToArray();
        for ( int i = 0; i < Dependencies.Length; i++ )
        { if (Dependencies[i].StartsWith(arg))
            { Dependencies[i] = Dependencies[i].Replace(arg, ""); } }
        return (CommandSplit[1], CommandSplit[2], Dependencies);
    }

    public static void CacheCommandIn(string command)
    {
        if (string.IsNullOrEmpty(command)) return;
        File.AppendAllLines(cmdCache, [command]);
    }

    public static string CacheCommandOut()
    {
        if (!File.Exists(cmdCache)) return string.Empty;
        var history = File.ReadAllLines(cmdCache).Where(l => !string.IsNullOrEmpty(l)).ToList();
        if (history.Count == 0) return string.Empty;
        int index = history.Count;
        var buffer = new StringBuilder();
        while (true)
        { var keyInfo = Console.ReadKey(intercept: true);
            if (keyInfo.Key == ConsoleKey.Enter)
            { Console.WriteLine();
                return buffer.ToString(); }
            if (keyInfo.Key == ConsoleKey.UpArrow)
            { if (index > 0) index--;
                ReplaceConsoleBuffer(buffer, history[index]);
                continue; }
            if (keyInfo.Key == ConsoleKey.DownArrow)
            { if (index < history.Count - 1) index++;
                else { index = history.Count;
                    ReplaceConsoleBuffer(buffer, string.Empty);
                    continue; }
                ReplaceConsoleBuffer(buffer, history[index]);
                continue; }
            if (keyInfo.Key == ConsoleKey.Backspace)
            { if (buffer.Length > 0)
                { buffer.Length--;
                    Console.Write("\b \b"); } 
                continue; }
            if (!char.IsControl(keyInfo.KeyChar))
            { buffer.Append(keyInfo.KeyChar);
                Console.Write(keyInfo.KeyChar); } }
    }

    static void ReplaceConsoleBuffer(StringBuilder buffer, string newText)
    {
        for (int i = 0; i < buffer.Length; i++) Console.Write("\b \b");
        buffer.Clear();
        buffer.Append(newText);
        Console.Write(newText);
    }
}
