# Clood: Claude AI-Powered Code Modification Tool

## Introduction

Clood is a powerful tool that combines the capabilities of Anthropic's Claude AI with Git version control. This application allows developers to leverage Claude's AI to suggest and implement changes to their codebase, all while maintaining version control best practices. Clood now offers both a command-line interface and a server mode for integration with other tools or workflows.

## Features

- Integrates Anthropic's Claude AI for code suggestions and modifications
- Supports both command-line and server modes
- Automatically creates new Git branches for AI-suggested changes
- Checks for uncommitted changes before running to ensure a clean working state
- Offers options to commit, abandon, or proceed with caution when uncommitted changes are present
- Applies AI-suggested changes to maintain code integrity
- Provides options to review, keep, or discard AI-suggested changes
- Handles merging of approved changes back into the original branch
- Supports custom system prompts for tailored AI behavior
- Allows specifying Git path and root directory
- Offers a version command to check the current Clood version

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

### Command-line Mode

1. Navigate to your project directory where you want to use Clood.

2. Run Clood with the desired options:
   ```
   dotnet run -- [options] <files>
   ```

   Options:
   - `-m, --server`: Start Clood in server mode
   - `-u, --server-urls`: Specify URLs for the server to listen on
   - `-v, --version`: Print the Clood version and exit
   - `-g, --git-path`: Specify the path to the Git executable
   - `-r, --git-root`: Specify the Git root directory (required)
   - `-p, --prompt`: Provide a prompt for Claude AI
   - `-s, --system-prompt`: Specify a file containing a system prompt for Claude AI

   Example:
   ```
   dotnet run -- -r /path/to/git/repo -p "Refactor this code for better performance" file1.cs file2.cs
   ```

3. Follow the prompts to review and apply changes.

### Server Mode

1. Start Clood in server mode:
   ```
   dotnet run -- -m -u http://localhost:5000
   ```

2. Use the following API endpoints:
   - `POST /api/clood/start`: Start a Clood session
   - `POST /api/clood/merge`: Merge or discard changes from a session
   - `POST /api/clood/revert`: Revert changes from a session

## How It Works

1. Clood checks for any uncommitted changes in your working directory.
2. It sends your specified files and prompt to Claude AI.
3. Claude analyzes the files and suggests changes.
4. Clood creates a new Git branch and applies Claude's suggestions.
5. You can review the changes and decide whether to keep or discard them.
6. If you choose to keep the changes, they can be merged back into your original branch.

## Contributing

Contributions to improve Clood are welcome! Please feel free to submit pull requests or open issues to suggest improvements or report bugs.

## License

GPL

## Disclaimer

This application interacts with your codebase and version control system. While it's designed to be safe and non-destructive, always ensure you have backups of your important data before running automated scripts on your codebase.

## Acknowledgments

Clood was heavily written by Claude AI under human supervision. While it serves as a proof of concept, a thorough review is recommended before using it in production environments.