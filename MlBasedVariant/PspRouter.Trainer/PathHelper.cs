namespace PspRouter.Trainer;

/// <summary>
/// Utility class for path operations
/// </summary>
public static class PathHelper
{
    /// <summary>
    /// Gets the solution root directory by looking for the .sln file
    /// </summary>
    /// <returns>The solution root directory path</returns>
    public static string GetSolutionRoot()
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var directory = new DirectoryInfo(currentDirectory);
        
        // Walk up the directory tree to find the solution file
        while (directory != null)
        {
            var solutionFiles = directory.GetFiles("*.sln");
            if (solutionFiles.Length > 0)
            {
                return directory.FullName;
            }
            
            directory = directory.Parent;
        }
        
        // Fallback to current directory if no solution file found
        return currentDirectory;
    }
    
    /// <summary>
    /// Resolves a relative path to an absolute path using the solution root as base
    /// </summary>
    /// <param name="relativePath">The relative path</param>
    /// <returns>The absolute path</returns>
    public static string ResolvePath(string relativePath)
    {
        if (Path.IsPathRooted(relativePath))
        {
            return relativePath; // Already absolute
        }
        
        var solutionRoot = GetSolutionRoot();
        return Path.Combine(solutionRoot, relativePath);
    }
}
