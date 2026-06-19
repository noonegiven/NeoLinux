namespace NeoLinux;
using System;
using System.Text;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using Microsoft.ML.OnnxRuntimeGenAI;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.Linq;


public class NeoSystem
{
    public static string? path = AppContext.BaseDirectory;
    private static bool IsBuilt { get; set; } = false;
    private static bool Panic;
    private static string NeoVersion { get; set; } = "Golden Gate";
    private static string KernVersion { get; set; } = "0.2.0";
    private static string shell { get; set; } = "NeoPrompt";
    private static string CachePath { get; set; } = Path.Combine(path, "cache");
    private static string KernelPath { get; set; } = Path.Combine(path, "cache", "sysKernel");
    private static bool isInstall { get; set; } = false;
    private static bool isCached { get; set; } = false;
    private static string cmdCache { get; } = Path.Combine(CachePath, "cacheCommand");

    public static void Main()
    {
        if (!isInstall && !isCached)
        { BuildSystem(IsBuilt);
            CheckKernelPanic(); }
        if (Panic) ShowKernelPanic();
        if (IsBuilt) ShowNeoIntro();
        while (true)
        {
            Console.Write($"Neo:{path}>");
            var inp = commands.CacheCommandOut();
            commands.CacheCommandIn(inp);
            bool CommandChecked = commands.CheckCommand(inp);
            if(CommandChecked)
            { var (c, a, d) = commands.HandleCommand(inp);
                if (c == "exit") return;
                if (c == "unknown") Console.WriteLine("ERROR: Failed to take cache");
                if (c == "open")
                { string arg = a.Trim().ToLower();
                    switch (arg)
                    { case "noise": NoiseEngine.Run(d); break;
                        case "neofetch": Console.Clear(); ShowNeoIntro(); break;
                        case "hermes": HermesEngine.Run(d); break;
                        case "chronos": ChronosEngine.Run(d); break;
                        case "mars": Console.Clear(); MarsEngine.Run(); break;
                        case "espresso": EspressoEngine.Run(d); break;
                        case "plume": PlumeEngine.Run(d); break;
                        case "connect": ConnectEngine.Run(d); break;
                        case "install": BuildSystem(IsBuilt); break;
                        default: Console.WriteLine("ERROR: Could not boot into app"); break; } }
                if (c == "ls")
                { try { string[] direct = Directory.GetDirectories(path); 
                    string[] files = Directory.GetFiles(path);
                    foreach (string dir in direct)
                    { Console.WriteLine($"[DIR] {Path.GetFileName(dir)}"); }
                    foreach (string file in files)
                    { Console.WriteLine($"[FILE] {Path.GetFileName(file)}"); } }
                    catch (Exception ex)
                    { Console.WriteLine($"ERROR: {ex.Message}"); } }
                if (c == "pwd")
                { Console.WriteLine($"[PWD] {Path.GetFileName(path)}"); }
                if (c == "rf")
                { if (!string.IsNullOrEmpty(a))
                    { string fl = a.Replace(commands.arg, "").Trim();
                        string scriptPath = Path.Combine(path, fl);
                        if (File.Exists(scriptPath))
                        { Console.WriteLine($"[NeoOS] Launching script compilation for: {fl}");
                            try { RunNeoFile(scriptPath, d); }
                            catch (Exception ex)
                            { Console.WriteLine($"ENGINE ERROR: Failed to spin up execution compiler. {ex.Message}"); } }
                        else { Console.WriteLine($"ERROR: Script file '{fl}' does not exist in appdata."); } }
                    else { Console.WriteLine("ERROR: You must provide a dependency to run 'rf'"); } }
                if (c == "rd")
                { try { string[] file = File.ReadAllLines(Path.Combine(path, a));
                        if (!File.Exists(Path.Combine(path, a)))
                        { Console.WriteLine("ERROR: Cannot find file");
                        } else { foreach (string l in file) { Console.WriteLine(l); } } }
                    catch { Console.WriteLine("ERROR: Cannot find file within directory"); } }
                if (c == "cd")
                { if(string.IsNullOrEmpty(a)) Console.WriteLine("ERROR: Cannot find directory");
                    if (a == "push")
                    { path = Path.GetDirectoryName(path); } 
                    else if (a == commands.arg)
                    { path = AppContext.BaseDirectory; }
                    else
                    { string checkPath = Path.GetFullPath(Path.Combine(path, a));
                        if (Directory.Exists(checkPath)) path = checkPath;
                        else Console.WriteLine("ERROR: Cannot find directory"); } }
                if (c == "rm")
                { if (a.Contains(".Neo"))
                    { File.Create(Path.Combine(path, a)); } 
                    else if (!a.Contains(".Neo"))
                    { Directory.CreateDirectory(Path.Combine(path, a)); } }
                if (c == "fm") File.Delete(Path.Combine(path, a));
                if (c == "ff")
                { string startDirectory = AppContext.BaseDirectory;
                    try { bool found = false;
                        var files = Directory.EnumerateFiles(startDirectory, a, SearchOption.AllDirectories);
                        foreach (var file in files) { Console.WriteLine($"File found at: <<{file}>>"); found = true; }
                        if (!found) Console.WriteLine("FILE ERROR: Could not find file(s)"); }
                    catch { Console.WriteLine("FILE ERROR: Could not figure out process"); } }
                
            }
            else
            { Console.WriteLine("ERROR: Commands must start with 'cmd: "); }
        }
    }

    private static void RunNeoFile(string scriptPath, string[] args)
    {
        string fileName = Path.GetFileName(scriptPath);
        string lowerName = fileName.ToLowerInvariant();
        if (lowerName.EndsWith(".sysneo") || lowerName.EndsWith(".neo.pa"))
        { Console.WriteLine("ERROR: Refusing to execute NeoOS system package files.");
            return; }
        if (lowerName.EndsWith(".neo.py"))
        { RunProcess("python3", [scriptPath, ..args], path ?? AppContext.BaseDirectory);
            return; }
        if (lowerName.EndsWith(".neo.cs"))
        { RunCSharpNeoFile(scriptPath, args);
            return; }
        if (lowerName.EndsWith(".neo"))
        { Console.WriteLine("ERROR: Plain .Neo files are documents. Use a runnable extension like .Neo.py or .Neo.cs.");
            return; }
        RunProcess(scriptPath, args, Path.GetDirectoryName(scriptPath) ?? path ?? AppContext.BaseDirectory);
    }

    private static void RunCSharpNeoFile(string scriptPath, string[] args)
    {
        string runRoot = Path.Combine(AppContext.BaseDirectory, "cache", "rf");
        string runName = Path.GetFileNameWithoutExtension(scriptPath).Replace(".", "_").Replace(" ", "_");
        string projectPath = Path.GetFullPath(Path.Combine(runRoot, runName));
        Directory.CreateDirectory(projectPath);
        CachePath = Path.Combine(AppContext.BaseDirectory, "cache");
        File.Copy(scriptPath, Path.Combine(projectPath, "Program.cs"), true);
        string framework = $"net10.0";
        string projectContents =
            "<Project Sdk=\"Microsoft.NET.Sdk\">\n" +
            "  <PropertyGroup>\n" +
            "    <OutputType>Exe</OutputType>\n" +
            $"    <TargetFramework>{framework}</TargetFramework>\n" +
            "    <ImplicitUsings>enable</ImplicitUsings>\n" +
            "    <Nullable>enable</Nullable>\n" +
            "  </PropertyGroup>\n" +
            "</Project>\n";
        File.WriteAllText(Path.Combine(projectPath, "NeoRun.csproj"), projectContents);
        string? dotnetPath = ResolveExecutable("dotnet");
        if (dotnetPath == null)
        { Console.WriteLine("ENGINE ERROR: Could not find the dotnet executable for .Neo.cs files.");
            Console.WriteLine("ENGINE ERROR: Add dotnet to PATH or set DOTNET_ROOT / DOTNET_HOST_PATH in the run configuration.");
            return; }
        RunProcess(dotnetPath, ["run", "--project", Path.Combine(projectPath, "NeoRun.csproj"), "--", ..args], projectPath);
    }

    private static string? ResolveExecutable(string executableName)
    {
        foreach (string candidate in GetExecutableCandidates(executableName))
        { if (File.Exists(candidate)) return candidate; }
        return null;
    }

    private static IEnumerable<string> GetExecutableCandidates(string executableName)
    {
        string? hostPath = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH");
        if (!string.IsNullOrWhiteSpace(hostPath)) yield return hostPath;
        string? dotnetRoot = Environment.GetEnvironmentVariable("DOTNET_ROOT");
        if (!string.IsNullOrWhiteSpace(dotnetRoot)) yield return Path.Combine(dotnetRoot, executableName);
        string? dotnetRootX64 = Environment.GetEnvironmentVariable("DOTNET_ROOT_X64");
        if (!string.IsNullOrWhiteSpace(dotnetRootX64)) yield return Path.Combine(dotnetRootX64, executableName);
        string? pathValue = Environment.GetEnvironmentVariable("PATH");
        if (!string.IsNullOrWhiteSpace(pathValue))
        { foreach (string pathEntry in pathValue.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
            { yield return Path.Combine(pathEntry, executableName); } }
        yield return Path.Combine("/usr/bin", executableName);
        yield return Path.Combine("/usr/local/bin", executableName);
        yield return Path.Combine("/snap/bin", executableName);
    }

    private static void RunProcess(string fileName, IEnumerable<string> args, string workingDirectory)
    {
        workingDirectory = Path.GetFullPath(workingDirectory);
        Directory.CreateDirectory(workingDirectory);
        var startInfo = new ProcessStartInfo
        { FileName = fileName,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false };
        foreach (string arg in args)
        { startInfo.ArgumentList.Add(arg); }
        using var process = Process.Start(startInfo);
        if (process == null)
        { Console.WriteLine("ENGINE ERROR: Failed to start runtime process.");
            return; }
        process.WaitForExit();
        if (process.ExitCode != 0)
        { Console.WriteLine($"ENGINE ERROR: Runtime exited with code {process.ExitCode}."); }
    }

    private static void BuildSystem(bool done)
    {
        bool ready = 
            Directory.Exists(Path.Combine(path, "cache")) && Directory.Exists(Path.Combine(path, "applications"));
        if (ready)
        { Console.WriteLine("Cache found and checking system");
            IsBuilt = true;
            return; }
        if (!done || !ready)
        { Directory.CreateDirectory(cmdCache);
            File.WriteAllText(Path.Combine(cmdCache, "cacheCommand.sysNeo"), "cmd: exit");
            Console.WriteLine("Building system and finding cache");
            var SystemState = new
            { chronVers = "12:98:09:SE", kernel = $"Neo:{path}>"};
            var dataState = new
            { espresso = "espressoEngine", hermes = "hermesEngine",
                mars = "marsEngine", noise = "noiseEngine",
                fetch = "neoFetchEngine", plume = "plumeEngine",
                connect = "connect", install = "install" };
            var txtState = new
            { e = $"{dataState.espresso} <><> {SystemState.chronVers} <><> {SystemState.kernel}",
                h = $"{dataState.hermes} <><> {SystemState.chronVers} <><> {SystemState.kernel}",
                m = $"{dataState.mars} <><> {SystemState.chronVers} <><> {SystemState.kernel}",
                n = $"{dataState.noise} <><> {SystemState.chronVers} <><> {SystemState.kernel}",
                f = $"{dataState.fetch} <><> {SystemState.chronVers} <><> {SystemState.kernel}",
                p = $"{dataState.plume} <><> {SystemState.chronVers} <><> {SystemState.kernel}",
                c = $"{dataState.connect} <><> {SystemState.chronVers} <><> {SystemState.kernel}" };
            Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "applications"));
            BuildApplicationData();
            string apppath = Path.Combine(AppContext.BaseDirectory, "applications");
            File.WriteAllText(Path.Combine(apppath, "espresso.sysNeo"), txtState.e);
            File.WriteAllText(Path.Combine(apppath, "hermes.sysNeo"), txtState.h);
            File.WriteAllText(Path.Combine(apppath, "marsEngine", "mars.sysNeo"), txtState.m);
            File.WriteAllText(Path.Combine(apppath, "noise.sysNeo"), txtState.n);
            File.WriteAllText(Path.Combine(apppath, "fetch.sysNeo"), txtState.f);
            File.WriteAllText(Path.Combine(apppath, "plume.sysNeo"), txtState.p);
            Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "cache"));
            string pcache = Path.Combine(AppContext.BaseDirectory, "cache");
            KernelPath = Path.Combine(pcache, "sysKernel");
            Directory.CreateDirectory(KernelPath);
            File.WriteAllText(Path.Combine(KernelPath, "sysKernel.sysNeo"), txtState.c);
            string? install = 
                " <12:98:09:SE\"NeoOS.[untitled]\">\n" +
                "  <GainedExclusivityFrom>\n" +
                "    <OutputType> .sysNeo + .Neo </OutputType>\n" +
                "    <TargetFramework>{.NeoFM}</TargetFramework>\n" +
                "    <ImplicitUsings>message</Pages>\n" +
                "    <Nullable>enable</Nullable>\n" +
                "  </PropertyOf>\n" +
                "</Project>\n";
            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "cache", "sysKernel", "install.sysNeo"), install);
            IsBuilt = true;
            Console.WriteLine("System built and found cache");
        } else Console.WriteLine("System built and returning cache");
    }

    private static void BuildApplicationData()
    {
        string _dir = Path.Combine(AppContext.BaseDirectory, "applications");
        Directory.CreateDirectory(Path.Combine(_dir, "plumeEngine"));
        Directory.CreateDirectory(Path.Combine(_dir, "plumeEngine", "appdata"));
        File.WriteAllText(Path.Combine(_dir, "plumeEngine", "appdata", "appdata.sysNeo"), KernVersion);
        Directory.CreateDirectory(Path.Combine(_dir, "chronosEngine"));
        Directory.CreateDirectory(Path.Combine(_dir, "chronosEngine", "appdata"));
        File.WriteAllText(Path.Combine(_dir, "chronosEngine", "appdata", "appdata.sysNeo"), KernVersion);
        File.WriteAllText(Path.Combine(_dir, "chronosEngine", "README.md"), "Do not touch these files!");
        Directory.CreateDirectory(Path.Combine(_dir, "hermesEngine"));
        Directory.CreateDirectory(Path.Combine(_dir, "hermesEngine", "appdata"));
        File.WriteAllText(Path.Combine(_dir, "hermesEngine", "appdata", "appdata.sysNeo"), KernVersion);
        Directory.CreateDirectory(Path.Combine(_dir, "espressoEngine"));
        Directory.CreateDirectory(Path.Combine(_dir, "espressoEngine", "appdata"));
        File.WriteAllText(Path.Combine(_dir, "espressoEngine", "appdata", "appdata.sysNeo"), KernVersion);
        Directory.CreateDirectory(Path.Combine(_dir, "marsEngine"));
        Directory.CreateDirectory(Path.Combine(_dir, "marsEngine", "appdata"));
        File.WriteAllText(Path.Combine(_dir, "marsEngine", "appdata", "appdata.sysNeo"), KernVersion);
        Directory.CreateDirectory(Path.Combine(_dir, "noiseEngine"));
        Directory.CreateDirectory(Path.Combine(_dir, "noiseEngine", "appdata"));
        File.WriteAllText(Path.Combine(_dir, "noiseEngine", "appdata", "appdata.sysNeo"), KernVersion);
    }

    private static void CheckKernelPanic()
    {
        string name = "sysKernel.sysNeo";
        string kernel = Path.Combine(KernelPath, name);
        if (File.Exists(kernel))
        { Console.WriteLine("Found kernel package");
            Panic = false; }
        else { Console.WriteLine("KERNEL ERROR: Cannot find kernel package!");
            Panic = true; }
    }

    private static void ShowKernelPanic()
    {
        Console.WriteLine("  _   _");
        Console.WriteLine($" | \\ | | ___  ___");
        Console.WriteLine($" |  \\| |/ _ \\/ _ \\");
        Console.WriteLine($" | |\\  |  __/ (_) |");
        Console.WriteLine(" |_| \\_|\\___|\\___/");
        var s = "=== KERNEL PANIC ===";
        Console.SetCursorPosition(Math.Max(0,(Console.WindowWidth - s.Length)/2), Math.Max(0,Console.WindowHeight/2));
        Console.WriteLine(s);
        s = "Cannot find kernel package";
        Console.SetCursorPosition(Math.Max(0,(Console.WindowWidth - s.Length)/2), Math.Max(0,Console.WindowHeight/2));
        Console.WriteLine(s);
        s = "Your system is corrupted and cannot be used";
        Console.SetCursorPosition(Math.Max(0,(Console.WindowWidth - s.Length)/2), Math.Max(0,Console.WindowHeight/2));
        Console.WriteLine(s);
        Console.WriteLine("");
        s = "'bro you can't delete any .sysNeo files' - charlie (NeoOS)";
        Console.SetCursorPosition(Math.Max(0,(Console.WindowWidth - s.Length)/2), Math.Max(0,Console.WindowHeight/2));
        Console.WriteLine(s);
        Console.CursorVisible = false;
        Thread.Sleep(Timeout.Infinite);
    }

    private static void ShowNeoIntro()
    {
        Console.WriteLine("  _   _                 ");
        Console.WriteLine($" | \\ | | ___  ___       OS: NeoLinux / NeoOS ({NeoVersion})");
        Console.WriteLine($" |  \\| |/ _ \\/ _ \\      Kernel Version: {KernVersion}");
        Console.WriteLine($" | |\\  |  __/ (_) |     Shell: {shell}");
        Console.WriteLine(" |_| \\_|\\___|\\___/      Author: 'Me!' (Deal With It)");
    }

    public static string BuildFileExtension(string content, string extend)
    {
        string returnFile = extend switch
        { "python" => $"{content}.Neo.py",
            "csharp" => $"{content}.Neo.cs",
            "path" => $"{content}.Neo.pa",
            "system" => $"{content}.sysNeo",
            _ => $"{content}.Neo" };
        return returnFile;
    }
}

public static class ConnectEngine
{
    public static void Run(string[] dep)
    {
        Console.Clear();
        Console.WriteLine("=== NeoOS Wireless Network Initializer ===");
        Console.Write("Enter Wi-Fi Network Name (SSID): ");
        string ssid = Console.ReadLine()?.Trim() ?? "";
        Console.Write("Enter Wi-Fi Password: ");
        string password = Console.ReadLine()?.Trim() ?? "";
        if (string.IsNullOrEmpty(ssid) || string.IsNullOrEmpty(password))
        { Console.WriteLine("ERROR: Network name and password cannot be blank."); return; }
        Console.WriteLine($"\n[NeoOS] Broadcasting connection request to router: {ssid}...");
        try
        { var proc = Process.Start("nmcli", $"device wifi connect \"{ssid}\" password \"{password}\"");
            proc?.WaitForExit();
            Console.WriteLine("[NeoOS] Hardware connection profile saved. Wi-Fi Active!"); }
        catch (Exception ex)
        { Console.WriteLine($"HARDWARE ERROR: Failed to talk to Linux network stack. {ex.Message}"); }
        Console.WriteLine("\nPress Enter to return to NeoPrompt.");
        Console.ReadLine();
        Console.Clear();
    }
}

public static class NoiseEngine
{
    private static string fileName { get; set; } = "untitled.Neo";
    private static string fullPath { get; set; }
    private static int row { get; set; } = 0;
    private static int col { get; set; } = 0;
    private static List<string> fileBuffer { get; set; }

    public static void Run(string[] dep)
    {
        Console.Clear();
        fileName = dep.Length > 0 ? dep[0] : NeoSystem.BuildFileExtension("untitled", "");
        fullPath =
        dep.Length > 0 ? Path.Combine(NeoSystem.path, fileName)
            : Path.Combine(AppContext.BaseDirectory, "applications", "noiseEngine", fileName);
        fileBuffer = File.Exists(fullPath)
        ? new List<string>(File.ReadAllLines(fullPath))
        : new List<string> { "" };
        row = 0; col = 0;
        int topLine = 0;
        bool needsRedraw = true;

        while (true)
        {
            int winW = Math.Max(1, Console.WindowWidth);
            int winH = Math.Max(1, Console.WindowHeight);

            // ensure row/col are inside buffer bounds
            if (row < 0) row = 0;
            if (row >= fileBuffer.Count) { fileBuffer.Add(""); row = fileBuffer.Count - 1; }
            if (col < 0) col = 0;
            if (col > fileBuffer[row].Length) col = fileBuffer[row].Length;

            // adjust viewport to keep cursor visible
            if (row < topLine) topLine = row;
            else if (row >= topLine + winH) topLine = row - winH + 1;
            topLine = Math.Max(0, topLine);

            if (needsRedraw)
            {
                for (int screenRow = 0; screenRow < winH; screenRow++)
                {
                    int bufIdx = topLine + screenRow;
                    string line = bufIdx < fileBuffer.Count ? fileBuffer[bufIdx] : "";
                    if (line.Length > winW) line = line.Substring(0, winW);
                    Console.SetCursorPosition(0, screenRow);
                    Console.Write(line.PadRight(winW));
                }
                needsRedraw = false;
            }

            int cursorRowOnScreen = Math.Max(0, Math.Min(winH - 1, row - topLine));
            int cursorColOnScreen = Math.Max(0, Math.Min(winW - 1, col));
            Console.SetCursorPosition(cursorColOnScreen, cursorRowOnScreen);

            var keyPress = Console.ReadKey(intercept: true);
            if (keyPress.KeyChar == '~')
            {
                File.WriteAllLines(fullPath, fileBuffer);
                Console.Clear();
                Console.WriteLine($"[Noise] Successfully wrote data to {fullPath}");
                break;
            }

            switch (keyPress.Key)
            {
                case ConsoleKey.LeftArrow:
                    if (col > 0) col--;
                    else if (row > 0) { row--; col = fileBuffer[row].Length; }
                    break;
                case ConsoleKey.RightArrow:
                    if (col < fileBuffer[row].Length) col++;
                    else if (row < fileBuffer.Count - 1) { row++; col = 0; }
                    break;
                case ConsoleKey.UpArrow:
                    if (row > 0) { row--; col = Math.Min(col, fileBuffer[row].Length); }
                    break;
                case ConsoleKey.DownArrow:
                    if (row < fileBuffer.Count - 1) { row++; col = Math.Min(col, fileBuffer[row].Length); }
                    break;
                case ConsoleKey.Home:
                    col = 0; break;
                case ConsoleKey.End:
                    col = fileBuffer[row].Length; break;
                case ConsoleKey.Enter:
                {
                    string toShift = fileBuffer[row].Substring(col);
                    fileBuffer[row] = fileBuffer[row].Substring(0, col);
                    fileBuffer.Insert(row + 1, toShift);
                    row++; col = 0;
                    needsRedraw = true;
                    break;
                }
                case ConsoleKey.Backspace:
                {
                    if (col > 0)
                    {
                        fileBuffer[row] = fileBuffer[row].Remove(col - 1, 1);
                        col--;
                    }
                    else if (row > 0)
                    {
                        int savedLen = fileBuffer[row - 1].Length;
                        fileBuffer[row - 1] += fileBuffer[row];
                        fileBuffer.RemoveAt(row);
                        row--; col = savedLen;
                    }
                    needsRedraw = true;
                    break;
                }
                case ConsoleKey.Delete:
                {
                    if (col < fileBuffer[row].Length)
                    {
                        fileBuffer[row] = fileBuffer[row].Remove(col, 1);
                    }
                    else if (row < fileBuffer.Count - 1)
                    {
                        fileBuffer[row] += fileBuffer[row + 1];
                        fileBuffer.RemoveAt(row + 1);
                    }
                    needsRedraw = true;
                    break;
                }
                case ConsoleKey.Tab:
                    fileBuffer[row] = fileBuffer[row].Insert(col, "    ");
                    col += 4;
                    needsRedraw = true;
                    break;
                default:
                    if (!char.IsControl(keyPress.KeyChar))
                    {
                        fileBuffer[row] = fileBuffer[row].Insert(col, keyPress.KeyChar.ToString());
                        col++;
                        needsRedraw = true;
                    }
                    break;
            }
        }
    }
}

public static class ChronosEngine
{
    private static Model model { get; set; }
    private static Tokenizer tokenizer { get; set; }
    private static string chatHistory { get; set; } = "";
    private static string currentSystemPrompt { get; set; }
    private static string modelFolder { get; set; }
    private static bool isIdeaMode { get; set; } = false;

    private const string GenPrompt = 
        "Your name is Chronos. You are a helpful, smart, and general-purpose AI assistant. Answer any question accurately.";
    private const string IdeaPrompt = 
        "Your name is Chronos. You are a sharp, project advisor. Critique the user's ideas, point out flaws, and ask challenging questions.";
    
    public static void Run(string[] dep)
    {
        Console.WriteLine("Initializing your local offline AI CHRONOS...");
        modelFolder = Path.Combine(NeoSystem.path, "applications", "chronosEngine", "ChronosEngine");
        try 
        { model = new Model(modelFolder);
          tokenizer = new Tokenizer(model);
          currentSystemPrompt = GenPrompt; }
        catch (Exception ex)
        { Console.WriteLine($"ERROR: Failed to load ONNX model file. {ex.Message}");
          return; }
        isIdeaMode = false;
        Console.Clear();
        Console.WriteLine("=== Local AI CHRONOS Active ===");
        Console.WriteLine("Commands: /idea (Switch to Bouncer), /gen (Switch to General), exit (Quit) [Press ESC mid-chat to stop]\n");
        while (true)
        { Console.ForegroundColor = isIdeaMode ? ConsoleColor.Cyan : ConsoleColor.Green;
            Console.Write(isIdeaMode ? "[IDEA BOUNCER] > " : "[GENERAL ASSISTANT] > ");
            Console.ResetColor();
            string input = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(input)) continue;
            if (input.ToLower() == "exit") { Console.Clear(); break; }
            if (input.ToLower() == "/idea")
            { isIdeaMode = true;
              currentSystemPrompt = IdeaPrompt;
              chatHistory = "";
              Console.WriteLine("\n*** Switched to Idea Bouncer Mode! RAM cleared. ***\n");
              continue; }
            if (input.ToLower() == "/gen")
            { isIdeaMode = false;
              currentSystemPrompt = GenPrompt;
              chatHistory = "";
              Console.WriteLine("\n*** Switched to General AI Mode! RAM cleared. ***\n");
              continue; }
            GenerateResponse(input);
            Console.WriteLine(); }
    }

    private static void GenerateResponse(string userInput)
    {
        if (string.IsNullOrEmpty(chatHistory))
        { chatHistory = $"<|system|>\n{currentSystemPrompt}<|end|>\n"; }
        chatHistory += $"<|user|>\n{userInput}<|end|>\n<|assistant|>\n";
        using var generatorParams = new GeneratorParams(model);
        ReadOnlySpan<int> inputIdsSpan = tokenizer.Encode(chatHistory)[0];
        ulong sequenceLength = (ulong)inputIdsSpan.Length;
        ulong batchSize = 1;
        generatorParams.SetInputIDs(inputIdsSpan, sequenceLength, batchSize);
        generatorParams.SetSearchOption("max_length", 2048);
        generatorParams.SetSearchOption("do_sample", false);
        using var generator = new Generator(model, generatorParams);
        using var tokenizerStream = tokenizer.CreateStream();
        StringBuilder newAnswerCache = new StringBuilder();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write("CHRONOS: ");
        Console.ResetColor();
        while (!generator.IsDone())
        {
            if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape)
            { Console.Write(" [Generation Stopped by User]");
              break; }
            generator.ComputeLogits();
            generator.GenerateNextToken();
            var outputTokens = generator.GetSequence(0);
            int newestTokenId = outputTokens[^1];
            string word = tokenizerStream.Decode(newestTokenId);
            newAnswerCache.Append(word);
            Console.Write(word);
            Thread.Sleep(10); 
        }
        chatHistory += $"{newAnswerCache}<|end|>\n";
    }
}

public static class HermesEngine
{
    private static TcpListener server { get; set; }
    private static TcpClient client { get; set; }
    private static NetworkStream networkStream { get; set; }
    private static byte[] receiveBuffer { get; set; } = new byte[1024];
    private static List<string> chatHistory { get; set; } = new List<string>();

    public static void Run(string[] dep)
    {
        Console.Clear();
        Console.WriteLine("=== Hermes Real-Time Network Core ===");
        Console.WriteLine("1. Host a chatroom (Be the Server)");
        Console.WriteLine("2. Join a friend's chatroom (Be the Client)");
        Console.WriteLine("Type 'exit' to quit.\n");
        Console.Write("Hermes> ");
        string choice = Console.ReadLine()?.Trim() ?? "";
        if (choice == "exit") { Console.Clear(); return; }
        if (choice == "1")
        { StartServer(); }
        else if (choice == "2")
        { StartClient(); }
        else
        { Console.WriteLine("ERROR: Invalid selection."); return; }
        LiveNetworkChatLoop();
    }

    private static void StartServer()
    {
        try
        { foreach (var ipAddress in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
            { if (ipAddress.AddressFamily == AddressFamily.InterNetwork)
                { Console.WriteLine($"[Hermes] Your Local IP Address is: {ipAddress}"); } }
            server = new TcpListener(IPAddress.Any, 8888);
            server.Start();
            Console.WriteLine("\n[Hermes] Waiting for your friend to connect over Wi-Fi...");
            client = server.AcceptTcpClient();
            networkStream = client.GetStream();
            Console.WriteLine("[Hermes] Friend Connected! Security tunnel open."); }
        catch (Exception ex)
        { Console.WriteLine($"NETWORK ERROR: {ex.Message}"); }
    }

    private static void StartClient()
    {
        Console.Write("\nEnter your friend's IP Address (e.g., 192.168.1.5): ");
        string ip = Console.ReadLine()?.Trim() ?? "";
        try
        { Console.WriteLine($"[Hermes] Dialing {ip} on port 8888...");
            client = new TcpClient();
            client.Connect(ip, 8888);
            networkStream = client.GetStream();
            Console.WriteLine("[Hermes] Connected successfully to your friend!"); }
        catch (Exception ex)
        { Console.WriteLine($"CONNECTION ERROR: {ex.Message}"); }
    }

    private static void LiveNetworkChatLoop()
    {
        Console.Clear();
        Console.WriteLine("--- Hermes Texting Center ---");
        Console.WriteLine("Type your message and hit Enter. Type ':q' to disconnect.\n");
        StringBuilder myCurrentTyping = new StringBuilder();
        chatHistory.Clear();
        while (true)
        { if (networkStream != null && networkStream.DataAvailable)
            { int bytesRead = networkStream.Read(receiveBuffer, 0, receiveBuffer.Length);
                if (bytesRead > 0)
                { string incomingText = Encoding.UTF8.GetString(receiveBuffer, 0, bytesRead);
                    chatHistory.Add($"FRIEND: {incomingText}");
                    Console.Clear();
                    Console.WriteLine("--- Live Wireless Network Chat Active ---");
                    Console.WriteLine("Type your message and hit Enter. Type ':q' to disconnect.\n");
                    foreach (string line in chatHistory)
                    { Console.WriteLine(line); }
                    Console.Write($"\nYOU> {myCurrentTyping}"); } }
            if (Console.KeyAvailable)
            { var keyInfo = Console.ReadKey(intercept: true);
                if (keyInfo.Key == ConsoleKey.Enter)
                { string msg = myCurrentTyping.ToString().Trim();
                    if (msg == ":q") { break; }
                    if (!string.IsNullOrEmpty(msg))
                    {if (networkStream != null)
                        { chatHistory.Add($"YOU: {msg}");
                            byte[] packet = Encoding.UTF8.GetBytes(msg);
                            networkStream.Write(packet, 0, packet.Length); }
                        else
                        { Console.WriteLine("\n[Hermes ERROR] No connection active. Text cannot be sent."); } }
                    myCurrentTyping.Clear();
                    Console.Clear();
                    Console.WriteLine("--- Live Wireless Network Chat Active ---");
                    Console.WriteLine("Type your message and hit Enter. Type ':q' to disconnect.\n");
                    foreach (string line in chatHistory) { Console.WriteLine(line); }
                    Console.Write($"\nYOU> "); }
                else if (keyInfo.Key == ConsoleKey.Backspace)
                { if (myCurrentTyping.Length > 0)
                { myCurrentTyping.Remove(myCurrentTyping.Length - 1, 1);
                    Console.Write("\b \b"); } }
                else if (!char.IsControl(keyInfo.KeyChar))
                { myCurrentTyping.Append(keyInfo.KeyChar);
                    Console.Write(keyInfo.KeyChar); } }
            System.Threading.Thread.Sleep(10);
        }
        networkStream?.Close();
        client?.Close();
        server?.Stop();
        Console.Clear();
    }
}

public static class EspressoEngine
{
    public static void Run(string[] dep)
    {
        Console.Clear();
        Console.WriteLine("Welcome to your Espresso!");
        Console.WriteLine("");
        Console.WriteLine("1. Create a list");
        Console.WriteLine("2. Remove a list");
        Console.WriteLine("3. View my lists");
        Console.WriteLine("4. Exit");
        Console.Write("Please select between 1 and 4: ");
        int opt = ChoosePart(1, 4);
        switch (opt)
        { case 1: Console.Clear(); CreateList(); break;
            case 2: Console.Clear(); RemoveList(); break;
            case 3: Console.Clear(); FindLists(); break;
            case 4: Console.Clear(); return; }
    }
    
    private static int ChoosePart(int min, int max)
    {
        while (true)
        { string? input = Console.ReadLine();
            if (int.TryParse(input, out int value) && value >= min && value <= max)
            { return value; }
            Console.Write("Please insert a number between " + min + " and " + max + ", thank you! "); }
    }

    private static void FindLists()
    {
        string root = AppContext.BaseDirectory;
        string folder = Path.Combine(root, "applications", "espressoEngine");
        string[] count = Directory.GetFiles(folder);
        for (int i = 0; i < count.Length; i++)
        { string name = Path.GetFileNameWithoutExtension(count[i]);
            Console.WriteLine($"{i}. {name}"); }
        Console.Write("Please pick your list (By name, case-sensitive): ");
        if (Console.ReadLine() == "q: exit") Run(count);
        string? opt = Console.ReadLine();
        ViewList(opt);
    }

    private static void CreateList()
    {
        string root = AppContext.BaseDirectory;
        string folder = Path.Combine(root, "applications", "espressoEngine");
        Directory.CreateDirectory(folder);
        Console.Write("Please provide a name: ");
        string? name = Console.ReadLine();
        string file = Path.Combine(folder, NeoSystem.BuildFileExtension(name, ""));
        if (!File.Exists(file))
        { File.WriteAllText(file, "");
            Console.WriteLine($"List '{name}' has been created.");
        } else
        { Console.WriteLine("There is already a list with that name."); }
        Console.Write("Press any key to continue");
        Console.ReadKey();
        Console.Clear();
        FindLists();
    }

    private static void RemoveList()
    {
        string root = AppContext.BaseDirectory;
        string folder = Path.Combine(root, "applications", "espressoEngine");
        string[] count = Directory.GetFiles(folder);
        for (int i = 0; i < count.Length; i++)
        { string name = Path.GetFileNameWithoutExtension(count[i]);
            Console.WriteLine($"{i}. {name}"); }
        Console.Write("Please select the list to remove (By name, case-sensitive): ");
        if (Console.ReadLine() == "q: exit") Run(count);
        string? opt = Path.Combine(folder, $"{Console.ReadLine()}.Neo");
        if (File.Exists(opt)) { File.Delete(opt); }
        else { Console.Clear();
            Console.WriteLine("ERROR: Did you select the right list?");
            Console.WriteLine("");
            RemoveList(); }
        string[] othercount = Directory.GetFiles(folder);
        if (othercount.Length == 0)
        { Console.Clear();
            Console.WriteLine("You have deleted all your lists!");
            Console.WriteLine("Automagically aborting and creating a list. . .");
            Console.WriteLine("");
            CreateList();
        } else { FindLists(); }
    }

    private static void ViewList(string? name)
    {
        string root = AppContext.BaseDirectory;
        string file = Path.Combine(root, "applications", "espressoEngine", $"{name}.Neo");
        Console.Clear();
        try
        { string[] entries = File.ReadAllLines(file);
            for (int i = 0; i < entries.Length; i++)
            {
                var isDone = entries[i].Contains("/");
                if (!isDone) Console.WriteLine($"{i}. {entries[i]}");
            }
            Console.WriteLine("");
            Console.WriteLine("Would you like to edit this list? (y/N): ");
            string opt = Console.ReadLine();
            switch (opt)
            { case "y":
                case "Y": ClearLines(2); EditList(name); break;
                case "n":
                case "N": FindLists(); break;
                default: FindLists(); break;
            } } catch (Exception ex)
        { Console.Clear();
            Console.WriteLine(ex);
            Console.WriteLine("ERROR: Did you select the right list?");
            FindLists(); }
    }

    private static void EditList(string? name)
    {
        string root = AppContext.BaseDirectory;
        string file = Path.Combine(root, "applications", "espressoEngine", $"{name}.Neo");
        Console.WriteLine("Please choose your action:");
        Console.WriteLine("1. Add entry");
        Console.WriteLine("2. Remove entry");
        int opt = ChoosePart(1, 2);
        switch (opt)
        { case 1:
                Console.Write("Please type your new entry: ");
                string? entry = Console.ReadLine();
                File.AppendAllLines(file, [entry]);
                ViewList(name); break;
            case 2: ClearLines(4); RemoveLine(name, file); break; }
    }

    private static void RemoveLine(string? name, string file)
    {
        string[] entries = File.ReadAllLines(file);
        Console.Write("Select the entry to remove. (Numeric, '/' to exit): ");
        string? tmp = Console.ReadLine();
        if (tmp == "/")
        { ViewList(name);
            return; }
        else if (int.TryParse(tmp, out int opt) && opt >= 0 && opt < entries.Length)
        { string input = $"/{entries[opt]}";
            entries[opt] = input;
            File.WriteAllLines(file, entries);
            ViewList(name);
            bool isDone = entries.All(entry => entry.Contains("/"));
            if (isDone) ViewList(name); }
    }

    private static void ClearLines(int count)
    {
        for (int i=0; i < count; i++)
        { if (Console.CursorTop > 0)
        { Console.SetCursorPosition(0, Console.CursorTop - 1);
            Console.Write("\x1b[2K"); } }
    }
}

public static class PlumeEngine
{
    private static string fileName { get; set; } = "plumeNote.Neo.plume";
    private static string fullPath { get; set; }
    private static int row { get; set; } = 0;
    private static int col { get; set; } = 0;
    private static List<string> fileBuffer { get; set; }

    public static void Run(string[] dep)
    {
        Console.Clear();
        fullPath = Path.Combine(NeoSystem.path, "applications", "plumeEngine");
        Directory.CreateDirectory(fullPath);
        fullPath = Path.Combine(NeoSystem.path, "applications", "plumeEngine", fileName);
        fileBuffer = File.Exists(fullPath) ? [..File.ReadAllLines(fullPath)] : [""];
        row = 0; col = 0;
        while (true)
        { Console.SetCursorPosition(0, 0);
            foreach (var t in fileBuffer)
            { Console.WriteLine(t.PadRight(Console.WindowWidth - 1)); }
            Console.SetCursorPosition(col, row);
            var keyPress = Console.ReadKey(intercept: true);
            if (keyPress.KeyChar == '~')
            { File.WriteAllLines(fullPath, fileBuffer);
                Console.Clear();
                Console.WriteLine($"[Plume] Successfully wrote data to {fullPath}");
                break; }
            switch (keyPress.Key)
            { case ConsoleKey.LeftArrow:
                    if (col > 0) col--;
                    else if (row > 0)
                    { row--; col = fileBuffer[row].Length; }
                    break;
                case ConsoleKey.RightArrow:
                    if (col < fileBuffer[row].Length) col++;
                    else if (row < fileBuffer.Count - 1)
                    { row++; col = 0; }
                    break;
                case ConsoleKey.UpArrow:
                    if (row > 0)
                    { row--; col = Math.Min(col, fileBuffer[row].Length); }
                    break;
                case ConsoleKey.DownArrow:
                    if (row < fileBuffer.Count - 1)
                    { row++; col = Math.Min(col, fileBuffer[row].Length); }
                    break;
                case ConsoleKey.Home: col = 0; break;
                case ConsoleKey.End: col = fileBuffer[row].Length; break;
                case ConsoleKey.Enter:
                    string textToShift = fileBuffer[row].Substring(col);
                    fileBuffer[row] = fileBuffer[row].Substring(0, col);
                    fileBuffer.Insert(row + 1, textToShift);
                    row++; col = 0; Console.Clear(); break;
                case ConsoleKey.Backspace:
                    if (col > 0)
                    { fileBuffer[row] = fileBuffer[row].Remove(col - 1, 1);
                        col--; }
                    else if (row > 0)
                    { int savedLength = fileBuffer[row - 1].Length;
                        fileBuffer[row - 1] += fileBuffer[row];
                        fileBuffer.RemoveAt(row);
                        row--; col = savedLength;
                        Console.Clear(); } break;
                case ConsoleKey.Delete:
                    if (col < fileBuffer[row].Length)
                    { fileBuffer[row] = fileBuffer[row].Remove(col, 1); }
                    else if (row < fileBuffer.Count - 1)
                    { fileBuffer[row] += fileBuffer[row + 1];
                        fileBuffer.RemoveAt(row + 1);
                        Console.Clear(); } break; }
            if (!char.IsControl(keyPress.KeyChar))
            { fileBuffer[row] = fileBuffer[row].Insert(col, keyPress.KeyChar.ToString());
                col++; } }
    }
}

public static class MarsEngine
{
    public static void Run()
    {
        
    }
}