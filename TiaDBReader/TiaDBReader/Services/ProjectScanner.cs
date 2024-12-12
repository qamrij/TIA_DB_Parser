using System;
using System.Collections.Generic;
using System.IO;
using TiaDBReader.Models;

namespace TiaDBReader.Services
{
    public class ProjectScanner
    {
        private readonly string _basePath;

        public ProjectScanner()
        {
            // Define the base path for TIA projects
            _basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TiaProjects");
        }

        /// <summary>
        /// Scans the TiaProjects folder for valid TIA Portal projects and returns a list of ProjectInfo objects.
        /// </summary>
        /// <returns>List of ProjectInfo objects</returns>
        public List<ProjectInfo> DiscoverProjects()
        {
            List<ProjectInfo> projects = new List<ProjectInfo>();

            // Ensure the directory exists
            if (!Directory.Exists(_basePath))
            {
                Console.WriteLine($"TiaProjects folder not found: {_basePath}");
                return projects;
            }

            // Define valid TIA Portal project file extensions, including .ap19 for TIA v19
            string[] validExtensions = { ".ap15", ".ap16", ".ap17", ".ap19" };

            // Search all files recursively in the base directory
            foreach (var file in Directory.GetFiles(_basePath, "*.*", SearchOption.AllDirectories))
            {
                var extension = Path.GetExtension(file);
                if (Array.Exists(validExtensions, ext => ext.Equals(extension, StringComparison.OrdinalIgnoreCase)))
                {
                    projects.Add(CreateProjectInfo(file));
                }
            }

            if (projects.Count == 0)
            {
                Console.WriteLine("No valid TIA projects found.");
            }

            return projects;
        }

        /// <summary>
        /// Creates a ProjectInfo object for a given project file.
        /// </summary>
        /// <param name="filePath">Path to the project file</param>
        /// <returns>ProjectInfo object</returns>
        private ProjectInfo CreateProjectInfo(string filePath)
        {
            ProjectInfo project = new ProjectInfo
            {
                ProjectPath = filePath,
                ProjectName = Path.GetFileNameWithoutExtension(filePath),
                LastModified = File.GetLastWriteTime(filePath),
                IsValid = true,
                ValidationMessage = "Valid project file."
            };

            // Additional validations can be added here
            if (!File.Exists(filePath))
            {
                project.IsValid = false;
                project.ValidationMessage = "File does not exist.";
            }

            return project;
        }
    }
}
