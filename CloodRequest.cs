﻿namespace Clood;

public class CloodRequest
{
    public string Prompt { get; set; }
    public string SystemPrompt { get; set; }
    public List<string> Files { get; set; }
    public string GitRoot { get; set; }
    public bool UseGit { get; set; } = true; // Default to true for backward compatibility
}