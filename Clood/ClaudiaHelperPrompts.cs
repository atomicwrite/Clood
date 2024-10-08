﻿namespace Clood;

public static class ClaudiaHelperPrompts
{
    public static string FormatCodeHelperPrompt(string filesDict, string prompt, string rootFolder)
        => $$"""
             You are tasked with applying a specific prompt to multiple code files and returning the modified contents in a JSON format. Here's how to proceed:

             1. You will be given a dictionary where the filename is the key and the value is the content, a prompt to apply, and the contents of multiple code files.

             2. The file dictionary is as follows:
             <file_dictionary>
             {{filesDict}}
              </file_dictionary>

             3. The prompt to apply to each file is:
             <prompt>
             {{prompt}}
             </prompt>

             4. Read all the files, some may not need to be changed and are just there for context:
               a. Generate the modified content based on the prompt
               b. If new files need to be created, include them in the output

             5. After processing all files, format your response as a JSON object with two arrays:
                - "changedFiles": An array of objects, each containing "filename" and "content" for modified existing files
                - "newFiles": An array of objects, each containing "filename" and "content" for newly created files
                - The Root folder is <root_folder>{{rootFolder}} </root_folder>.
                - If you are asked to put new file in a specific folder, try to figure it out based on the root_folder.
                - All files should have the same root folder as root_folder. 
                
             6. Ensure that the JSON is properly formatted and escaped, especially for multi-line code contents.

             Here's an example of how your output should be structured:

             ```json
             {
               "changedFiles": [
                 {"filename": "file1.py", "content": "# Modified content of file1.py\n..."},
                 {"filename": "file2.js", "content": "// Modified content of file2.js\n..."}
               ],
               "newFiles": [
                 {"filename": "newfile.cpp", "content": "// Content of new file\n..."}
               ]
             }
             ```

             Remember to include all modified files in the "changedFiles" array and any new files in the "newFiles" array.
             """;
}