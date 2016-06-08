<Query Kind="Program" />

void Main()
{
	var patterns = new List<string> {
		"te*t",
		"tes*",
		"t*s*",
		"test/*/two",
		"test/**/two",
		"test/**",
		"**/two",
		"test/one/*/three/**",
		"test/one/*/th*e/**",
		"*.txt"
	};

	var texts = new List<string> {
		"test",
		"test/one/tmp/two",
		"test/one/tmp/fish/two",
		"test/one/two/three/four",
		"test.txt",
		"test/test.txt",
		"test.txtfile"
	};
	
	texts.ForEach(t => patterns.ForEach(p => { 
		var match = MatchPattern(p, t);
		var m = (match == MATCH) ? "MATCH" : match.ToString(); 
		if(match == MATCH)
			string.Format("{0} -> {1} = {2}", t, p, m).Dump(); 
	}));
	
	// MatchPattern("test/one/*/three/**", "test/one/two/three/four").Dump();
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

    for (; p < p_len; p++, t++)
    {
		if (t > t_EOP)
			return NOMATCH;

        char p_ch = pattern[p];
        char t_ch = text[t];
        
        bool match_slash;
        
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
                if ((p + 1) < p_len && pattern[p + 1] == '*')
                {
                    // Figure out what the character *before* the first star is
                    // (using null char to represent the beginning of the pattern)
                    char pre_star_ch = (p - 1) >= 0 ? pattern[p - 1] : '\0';
                    // Advance through the pattern until we get to something which *isn't* a star
                    while (p < p_EOP && (p_ch = pattern[++p]) == '*') { }
                    // If PATHNAME isn't set, a single star also matches slashes
                    if (!flags.HasFlag(MatchFlags.PATHNAME))
                    {
                        match_slash = true;
                    }
                    // If the character before the first star is either the beginning of the pattern or a slash
                    else if (pre_star_ch == '\0' || pre_star_ch == '/')
					{
						if (p_ch == '/' && Match(pattern, text, p + 1, t, flags) == MATCH)
							return MATCH;

						match_slash = true;
                    }
                    else
                    {
                        // The pattern is invalid (double-star wildcards are only valid 
                        // if bounded by a slash on at least one side)
                        return ABORT_MALFORMED;
                    }
                }
                else
                {
                    // It's only a single star, so use PATHNAME to determine whether to match slashes
                    match_slash = !flags.HasFlag(MatchFlags.PATHNAME);
                }
				
				if (p_ch == '*' && p < p_EOP)
				{
					p_ch = pattern[++p]; 
					t_ch = text[++t];
				}

                // If we're at the end of the pattern
                if (p == p_EOP)
                {
                    // If there was only one star and PATHNAME was set, match_slash will be false
                    
                    // Trailing "*" matches only if there are no more slash characters
                    if (!match_slash && text.Contains('/'))
                        return NOMATCH;
                    
                    // Trailing "**" matches everything
                    return MATCH;
				}
				else if  (!match_slash && p_ch == '/') 
				{
					int nextSlashIndex = Array.IndexOf(text, '/', t);
					if (nextSlashIndex == -1)
						return NOMATCH;
						
					t = nextSlashIndex;
					break;
				}

				
				
                int match;
                
                while (true)
                {
                    if(t == t_len)
                        break;

					if ((match = Match(pattern, text, p, t, flags)) != NOMATCH)
					{
						if(!match_slash || match != ABORT_TO_STARSTAR)
							return match;
					}
					else if (!match_slash && t_ch == '/')
					{
						return ABORT_TO_STARSTAR;
					}
					
					t++;
                }
                
                return ABORT_ALL;
        }
    }
    
    return t == text.Length ? MATCH : NOMATCH;
}