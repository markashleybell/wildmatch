<Query Kind="Program" />

void Main()
{
    var tests = File.ReadAllLines(basePath + @"\tests.txt")
                    .Where(l => !l.StartsWith("#") && !string.IsNullOrWhiteSpace(l))
                    .Select(l => l.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
                    .Select(l => new Test { Text = l[2], Pattern = l[3] })
                    .ToList();

    tests.ForEach(t => {
        var result = ReferenceMatchPattern(t.Pattern, t.Text, true);
        string.Format("{0}: {1}", t.Pattern, result).Dump();
    });
}

public class Test
{
    public string Text { get; set; }
    public string Pattern { get; set; }
}

static string basePath = Path.GetDirectoryName(Util.CurrentQueryPath);

public Process CreateProcess(string executableFilename, string arguments, string workingDirectory)
{
    return new Process {
        EnableRaisingEvents = true,
        StartInfo = new ProcessStartInfo
        {
            FileName = executableFilename,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        }
    };
}

public int ReferenceMatchPattern(string pattern, string text, bool caseFold)
{
    var workingDirectory = basePath + @"\c";
    
    var log = new List<string>();
    
    var arguments = pattern + " " + text + " " + (caseFold ? 1 : 0);
    
    using (var build = CreateProcess(workingDirectory + @"\wm.exe", arguments, workingDirectory))
    {
        build.Start();

        build.OutputDataReceived += (sender, e) => log.Add("0> " + e.Data);
        build.BeginOutputReadLine();

        build.ErrorDataReceived += (sender, e) => log.Add("1> " + e.Data);
        build.BeginErrorReadLine();

        build.WaitForExit();
        
        return build.ExitCode;
    }
}