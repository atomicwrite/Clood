namespace Clood;

public static class ClaudiaHelperPrompts
{
    public static string FormatPromptHelperPrompt(string filesDict, string prompt,   string folderLayoutYaml ) =>
         $$"""
            You are an AI assistant tasked with helping users improve their prompts for AI-assisted coding tasks. Your goal is to analyze the given information and suggest an upgraded prompt that will lead to better results.
            
            First, review the project layout:
            <project_layout>
            {{folderLayoutYaml}}
            </project_layout>
            
            Next, examine a dictionary where the filename is the key and the value is the content relevant files:
            <file_contents>
            {{filesDict}}
            </file_contents>
            
            Now, consider the original prompt the user was thinking about:
            <original_prompt>
            {{prompt}}
            </original_prompt>
            
            To create an improved prompt, follow these steps:
            
            1. Analyze the project layout to understand the structure and context of the codebase.
            2. Review the file contents to identify specific code elements, patterns, or issues that need to be addressed.
            3. Evaluate the original prompt to determine its strengths and weaknesses.
            4. Consider the following aspects when formulating the improved prompt:
               a. Clarity and specificity of the request
               b. Relevance to the project structure and file contents
               c. Inclusion of necessary context and constraints
               d. Guidance on desired output format or style
               e. Any potential edge cases or considerations
            
            5. Craft an upgraded prompt that addresses the identified areas for improvement and incorporates the relevant information from the project layout and file contents.
            
            Present your improved prompt within <improved_prompt> tags. After the improved prompt, provide a brief explanation of the changes and improvements you made, enclosed in <explanation> tags.
            
            Remember, your task is not to complete the coding task itself, but to create a better prompt that will guide an AI assistant in completing the task more effectively.
            """;
    
    public static string FormatCodeHelperPrompt(string filesDict, string prompt, string rootFolder, string folderLayoutYaml)
        => $$"""
             You are tasked with applying a specific prompt within the context multiple code files and returning the modified contents in a JSON format. Here's how to proceed:

             1. You will be given a dictionary where the filename is the key and the value is the content, a prompt to apply, and the contents of multiple code files.

             2. The file dictionary is as follows:
             <file_dictionary>
             {{filesDict}}
              </file_dictionary> 
              
             8. A YAML representation of the entire project folder, including file sizes and last modified dates, is provided below:
             <folder_layout>
             {{folderLayoutYaml}}
             </folder_layout>
         

             4. Read all the files, some may not need to be changed and are just there for context:
               a. Generate the modified content based on the prompt
               b. If new files need to be created, include them in the output
               c. The Root folder is <root_folder>{{rootFolder}} </root_folder>.
               d. If you are asked to put new file in a specific folder, try to figure it out based on the root_folder.
               e. All files should have the same root folder as root_folder.

             5. Follow these guidelines for c# for each file.
              
              a. Follow C# coding conventions and best practices
              b. Implement proper exception handling and use nullable reference types
              c. Think step by step based on the prompt
              d. Think about the problem in a composable way.
              e. Always fill out classes never leave them empty. Think about each one.
              
             6. After processing all files, format your response as a JSON object with two arrays:
                - "changedFiles": An array of objects, each containing "filename" and "content" for modified existing files
                - "newFiles": An array of objects, each containing "filename" and "content" for newly created files
                - "answered": If you were able to answer the question or not
                - Do not include a file if it has not been changed. 
                
             7. Ensure that the JSON is properly formatted and escaped, especially for multi-line code contents.

             8. The prompt to apply to each file is:
             <prompt>
             {{prompt}}
             </prompt>

             9. The path for the folder layout is the same as the root folder: <root_folder>{{rootFolder}} </root_folder>

             Here's an example of how your output should be structured:

             ```json
             {
               "changedFiles": [
                 {"filename": "file1.py", "content": "# Modified content of file1.py\n..."},
                 {"filename": "file2.js", "content": "// Modified content of file2.js\n..."}
               ],
               "newFiles": [
                 {"filename": "newfile.cpp", "content": "// Content of new file\n..."}
               ],
               "answered": true
             }
             ```
             10. Set the answered property to true only if you know the answer or can make a well-informed guess;
              otherwise set the answered property  to false. If you can't answer, leave the changedFiles and newFiles 
              as empty arrays.

             11. Remember to include all modified files in the "changedFiles" array and any new files in the "newFiles" array.
             
             12. THINK THINK THINK THINK THINK THINK
             """;
}