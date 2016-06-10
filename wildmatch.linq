<Query Kind="Program" />

void Main()
{
	var patterns = new List<string> {
		"te*t",
		"tes*",
		"t*s*",
		"test/*/two",
		"test/*/*/two",
		"test/**/two",
		"test/**",
		"**/two",
		"test/one/*/three/**",
		"test/one/*/th*e/**",
		"test/*",
		"*.txt",
        "**/test",
        "**/sub2/**.txt",
        "**/sub2/*.txt",
        "sub1/**/test"
	};

	var texts = new List<string> {
		"test",
		"test/one/tmp/two",
		"test/one/tmp/fish/two",
		"test/one/two/three/four",
		"test.txt",
		"test/test.txt",
		"test.txtfile",
        "sub1/test",
        "sub1/sub2/test.txt"
	};
	
	var divider = "------------------------------------------------";
    var br = Environment.NewLine;
	
    var runAllTests = false;
    var logMatches = false;
    
    if(runAllTests) 
    {
        var output = new List<string>();
        
    	patterns.ForEach(p => {
            var results = new List<string>();
            
    		texts.ForEach(t => { 
    			var match = MatchPattern(p, t);
                var refmatch = ReferenceMatchPattern(p, t);
    
                if (match != refmatch || (logMatches && (match == MATCH && refmatch == MATCH)))
                {
                    var m = (match == MATCH) ? "MATCH" : match.ToString();
                    var rm = (refmatch == MATCH) ? "MATCH" : refmatch.ToString();
                    results.Add(string.Format("prt -> {0} {1}", m, t));
                    results.Add(string.Format("ref -> {0} {1}", rm, t));
                    results.Add(divider);
                }
    		});

            if (results.Count > 0)
            {
                output.Add(br);
                output.Add(p);
                output.Add(divider);
                output.AddRange(results);
            }
    	});
        
        string.Join(br, output).Dump();
    }

    MatchPattern("**/test", "test").Dump();

//    MatchPattern("te*t", "test.txtfile").Dump(); // Should not(?!) be match
//    MatchPattern("test/*", "test/test.txt").Dump(); // Should be match
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

public int ReferenceMatchPattern(string pattern, string text)
{
    var workingDirectory = basePath + @"\c";
    
    var log = new List<string>();
    
    using (var build = CreateProcess(workingDirectory + @"\wm.exe", pattern + " " + text, workingDirectory))
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

static int ABORT_MALFORMED = 2;
static int NOMATCH = 1;
static int MATCH = 0;
static int ABORT_ALL = -1;
static int ABORT_TO_STARSTAR = -2;

[Flags]
public enum MatchFlags
{
    /// <summary>
    /// Patterns are case-sensitive, single asterisks in patterns match path slashes
    /// </summary>
    NONE = 0,
    /// <summary>
    /// If set, pattern matches are case-insensitive
    /// </summary>
    IGNORE_CASE = 1,
    /// <summary>
    /// If set, single asterisks in patterns should not match path slashes
    /// </summary>
    PATHNAME = 2
}

public bool IsGlobSpecial(char c)
{
    return c == '*' || c == '?' || c == '[' || c == '\\';
}

public int MatchPattern(string pattern, string text)
{
    return Match(pattern.ToCharArray(), text.ToCharArray(), 0, 0, MatchFlags.PATHNAME);
}

public int Match(char[] pattern, char[] text, int p, int t, MatchFlags flags)
{
    int p_len = pattern.Length;
    int p_EOP = p_len - 1;

    int t_len = text.Length;
    int t_EOP = t_len - 1;
	
//	("pat: " + new String(pattern, p, p_len - p)).Dump();
//	("txt: " + new String(text, t, t_len - t)).Dump();

    char p_ch;

    for (; p < p_len && (p_ch = pattern[p]) != -1; p++, t++)
    {
        int match;
        bool match_slash;
        
        char t_ch;
        
		if (t == t_len && p_ch != '*')
			return ABORT_ALL;
            
        t_ch = text[t];

        switch (p_ch)
        {
            // Escape character: require literal match of next char
            case '\\': 
                p_ch = pattern[++p];
                goto default;
            // Normal character: literal match
            default: 
                if (t_ch != p_ch)
                    return NOMATCH;
                continue;
            // Match any character except slash
            case '?': 
                if (t_ch == '/')
                    return NOMATCH;
                continue;
            // Match any character unless PATHNAME flag is set, then match any char except slash
            case '*': 
                // If the *next* character is a star as well...
                if ((++p) < p_len && pattern[p] == '*')
                {
                    // Figure out what the character *before* the first star is
                    // (using null char to represent the beginning of the pattern)
                    char pre_star_ch = (p - 2) >= 0 ? pattern[p - 2] : '\0';
                    // Advance through the pattern until we get to something which *isn't* a star
                    while ((++p) < p_len && pattern[p] == '*') { }
                    // If PATHNAME isn't set, a single star also matches slashes
                    if (!flags.HasFlag(MatchFlags.PATHNAME))
                    {
                        match_slash = true;
                    }
                    // If the character before the first star is either the beginning of the pattern or a slash,
                    // and the character after the last star is either the end of the pattern or a slash
                    else if ((pre_star_ch == '\0' || pre_star_ch == '/') && (p == p_len || pattern[p] == '/'))
					{
						if (pattern[0] == '/' && Match(pattern, text, p + 1, t, flags) == MATCH)
							return MATCH;

						match_slash = true;
                    }
                    else
                    {
                        // The pattern is invalid (double-star wildcards are only valid 
                        // if bounded by slashes or beginning/end of line)
                        return ABORT_MALFORMED;
                    }
                }
                else
                {
                    // It's only a single star, so use PATHNAME to determine whether to match slashes
                    match_slash = !flags.HasFlag(MatchFlags.PATHNAME);
				}
				
                // If we're at the end of the pattern
                if (p == p_len)
                {
                    // If there was only one star and PATHNAME was set, match_slash will be false
                    // Trailing "*" matches only if there are no more slash characters
                    if (!match_slash && text.Contains('/'))
                        return NOMATCH;
                    
                    // Trailing "**" matches everything
                    return MATCH;
				}
				else if  (!match_slash && pattern[p] == '/') 
				{
					// We're at a slash, so consume the text until the next slash
					int nextSlashIndex = Array.IndexOf(text, '/', t);
					// If there aren't any more slashes, this can't be a match
					if (nextSlashIndex == -1)
						return NOMATCH;
						
					t = nextSlashIndex;
					break;
				}
                
				// Try to match the remaining text
				// Each time the match fails, remove the first character from the text and retry
                while (true)
                {
                    if(t == t_EOP)
                        break;

                    // Try to advance faster when an asterisk is followed by a literal. 
                    // We know in this case that the string before the literal must belong to "*".
                    // If match_slash is false, do not look past the first slash as it cannot belong to '*'.
                    if (!IsGlobSpecial(pattern[p]))
                    {
                        p_ch = pattern[p];
                        while (t < t_len && ((t_ch = text[t]) != '/' || match_slash))
                        {
                            if (t_ch == p_ch)
                                break;
                                
                            t++;
                        }
                        if (t_ch != p_ch)
                            return NOMATCH;
                    }

                    if ((match = Match(pattern, text, p, t, flags)) != NOMATCH)
					{
						if(!match_slash || match != ABORT_TO_STARSTAR)
							return match;
					}
					else if (!match_slash && t_ch == '/')
					{
						return ABORT_TO_STARSTAR;
					}
					
                    t_ch = text[++t];
                }
                
                return ABORT_ALL;
        }
    }
    
    return t == text.Length ? MATCH : NOMATCH;
}