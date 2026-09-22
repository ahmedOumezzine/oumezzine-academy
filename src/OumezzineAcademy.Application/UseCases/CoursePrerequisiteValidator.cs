using OumezzineAcademy.Application.Abstractions;

namespace OumezzineAcademy.Application.UseCases;

public sealed class CoursePrerequisiteValidator(ICoursePrerequisiteEdges edges) : ICoursePrerequisiteValidator
{
    public async Task<bool> WouldCreateCycleAsync(Guid courseId, IEnumerable<Guid> prerequisites, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(prerequisites);
        var existingEdges = await edges.GetAllAsync(cancellationToken);
        var graph = existingEdges
            .Where(edge => edge.CourseId != courseId)
            .GroupBy(edge => edge.CourseId)
            .ToDictionary(group => group.Key, group => group.Select(edge => edge.PrerequisiteCourseId).ToList());
        graph[courseId] = prerequisites.Distinct().ToList();

        var visiting = new HashSet<Guid>();
        var visited = new HashSet<Guid>();

        bool HasCycle(Guid node)
        {
            if (visiting.Contains(node)) return true;
            if (!visited.Add(node)) return false;

            visiting.Add(node);
            if (graph.TryGetValue(node, out var next) && next.Any(HasCycle)) return true;
            visiting.Remove(node);
            return false;
        }

        return graph.Keys.Any(HasCycle);
    }
}