using Microsoft.Build.Framework;
using NuGet.Versioning;

namespace Polyadic.Build.SemanticVersioning;

public sealed class GenerateSemanticVersionRanges : Microsoft.Build.Utilities.Task
{
    [Required]
    public ITaskItem[] ProjectReferencesWithVersions { get; set; } = null!;

    public string VersionMetadataName { get; set; } = "ProjectVersion";

    [Output]
    public ITaskItem[] ProjectReferencesWithExactVersions { get; set; } = null!;

    public override bool Execute()
    {
        foreach (var projectReference in ProjectReferencesWithVersions)
        {
            var versionString = projectReference.GetMetadata(VersionMetadataName);

            if (!SemanticVersion.TryParse(versionString, out var version))
            {
                Log.LogError("'{0}' is not a valid semantic version", versionString);
                return false;
            }

            projectReference.SetMetadata(VersionMetadataName, ToVersionRange(version));
        }

        ProjectReferencesWithExactVersions = ProjectReferencesWithVersions;
        return true;
    }

    private static string ToVersionRange(SemanticVersion version)
        => version switch
        {
            /* No SemVer guarantees for pre-release (e.g. nightly) versions, so we use an exact version. */
            { IsPrerelease: true } => $"[{version}]",
            /* We use Cargo's convention for 0.x versions i.e. minor = breaking, patch = feature or patch. */
            { Major: 0, Minor: var minor, Patch: var patch } => $"[0.{minor}.{patch}, 0.{minor + 1})",
            /* 1.x versions follow regular SemVer rules. */
            { Major: var major, Minor: var minor, Patch: var patch } => $"[{major}.{minor}.{patch}, {major + 1})",
        };
}
