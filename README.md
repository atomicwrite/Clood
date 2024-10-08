# Claude Git Integration Script

## Introduction

The Claude Git Integration Script is a powerful tool that combines the capabilities of Anthropic's Claude AI with Git version control. This script allows developers to leverage Claude's AI to suggest and implement changes to their codebase, all while maintaining version control best practices.

## Features

- Integrates Anthropic's Claude AI for code suggestions and modifications
- Automatically creates new Git branches for AI-suggested changes
- Checks for uncommitted changes before running to ensure a clean working state
- Offers options to commit, abandon, or proceed with caution when uncommitted changes are present
- Applies AI-suggested changes as patches to maintain code integrity
- Provides options to review, keep, or discard AI-suggested changes
- Handles merging of approved changes back into the original branch

## Prerequisites

- .NET 6.0 or later
- Git installed and configured on your system
- An Anthropic API key for Claude AI

## Setup

1. Clone the repository:
   ```
   git clone https://github.com/atomicwrite/clood.git
   cd clood
   ```

2. Install the required NuGet packages:
   ```
   dotnet restore
   ```

3. Set up your Anthropic API key as a user secret:
   ```
   dotnet user-secrets set "clood-key" "your-api-key-here"
   ```

## Usage

1. Navigate to your project directory where you want to use the script.

2. Run the script, providing the files you want Claude to analyze and modify:
   ```
   dotnet run -- path/to/file1.cs path/to/file2.cs
   ```

3. Follow the prompts to:
   - Commit any uncommitted changes (if present)
   - Enter your prompt for Claude
   - Review Claude's suggested changes
   - Apply or discard the changes
   - Merge the changes into your original branch (if desired)

## How It Works

1. The script first checks for any uncommitted changes in your working directory.
2. It then sends your specified files and prompt to Claude AI.
3. Claude analyzes the files and suggests changes.
4. The script creates a new Git branch and applies Claude's suggestions as a patch.
5. You can review the changes and decide whether to keep or discard them.
6. If you choose to keep the changes, they can be merged back into your original branch.

## Contributing

Contributions to improve the Claude Git Integration Script are welcome! Please feel free to submit pull requests or open issues to suggest improvements or report bugs.

## License

GPL

## Disclaimer

This script interacts with your codebase and version control system. While it's designed to be safe and non-destructive, always ensure you have backups of your important data before running automated scripts on your codebase.


## Disclaimer 2

Heavily written by claude under my supervision. I don't haven't done a proper review because this is POC. 
