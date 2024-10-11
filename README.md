# Clood

Clood is a tool that integrates Claude AI with your local development environment, allowing you to make AI-assisted changes to your codebase.

## Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later

If you don't have .NET installed, visit the link above and follow the installation instructions for your operating system.

## Building Clood for Release
 
1. Clone the repository:
   ```
   git clone https://github.com/atomicwrite/Clood.git
   cd Clood
   ```

2. Build the project for release:
   ```
   dotnet publish -c Release
   ```
 

The release build will be available in the `bin/Release/net8.0/publish` directory.

## Adding a .NET Secret (Optional)

To securely store your API key, you can use .NET's secret manager:
 
1. Initialize the user secrets for the project:
   ```
   dotnet user-secrets init
   ```

2. Add your API key:
   ```
   dotnet user-secrets set "clood-key" "your-api-key-here"
 
   ```

Note: You can skip this step for now and use an environment variable instead (see below), but this method will be phased out in future versions.

## Setting up the API Key

For now, you can set the API key as an environment variable:

- On Windows (Command Prompt):
  ```
  set clood-key=your-api-key-here
  ```

- On macOS/Linux:
  ```
  export clood-key=your-api-key-here
  ```

Remember to replace `your-api-key-here` with your actual API key.

 
## Starting the Server

To start the Clood server:

1. Navigate to the publish directory:
   ```
   cd bin/Release/net8.0/publish
 

2. Run the server:
   ```
   dotnet Clood.dll -m -r /path/to/your/git/repo server
   ```

Replace `/path/to/your/git/repo` with the actual path to your Git repository.

Note: The `server` keyword at the end is required to start Clood in server mode.

## Clood Options

Clood supports various command-line options:

- `-m` or `--server`: Start Clood in server mode.
- `-u` or `--server-urls`: Specify the URLs for the minimal API to run on.
- `-v` or `--version`: Print the version of Clood and exit.
- `-g` or `--git-path`: Specify an optional path to the Git executable.
- `-r` or `--git-root`: Specify the Git root directory (required).
- `-p` or `--prompt`: Provide a prompt for Claude AI.
- `-s` or `--system-prompt`: Specify a file containing a system prompt for Claude AI.
- Files: You can list files to process after the options.

Example usage:
```
dotnet Clood.dll -m -r /path/to/repo -u http://localhost:5000 server
```

This starts the server on `http://localhost:5000` with the Git root set to `/path/to/repo`.

## Executable Path

After building, the Clood executable will be located at:

```
bin/Release/net8.0/publish/Clood.dll
```

You can run this directly with the `dotnet` command as shown in the "Starting the Server" section.

## Releases

We provide pre-built releases for easier installation. Check out our [Releases page](https://github.com/atomicwrite/Clood/releases) to download the latest version.

To use a release:

1. Download the release for your platform
2. Extract the contents
3. Run the executable as described in the "Starting the Server" section

## Getting Help

If you encounter any issues or have questions, please open an issue on our GitHub repository at https://github.com/atomicwrite/Clood.

Happy coding with Clood!
