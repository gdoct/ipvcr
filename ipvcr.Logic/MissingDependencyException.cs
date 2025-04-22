using System.Diagnostics.CodeAnalysis;

namespace ipvcr.Logic
{
    [ExcludeFromCodeCoverage]
    public class MissingDependencyException : Exception
    {

        public string DependencyName { get; }

        public MissingDependencyException(string dependencyName)
            : base($"The required dependency '{dependencyName}' is missing.")
        {
            DependencyName = dependencyName;
        }

        public MissingDependencyException(string dependencyName, string message)
            : base(message)
        {
            DependencyName = dependencyName;
        }

        public MissingDependencyException(string dependencyName, string message, Exception innerException)
            : base(message, innerException)
        {
            DependencyName = dependencyName;
        }
    }
}