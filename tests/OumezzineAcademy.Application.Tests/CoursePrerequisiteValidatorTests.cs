using Xunit;
using OumezzineAcademy.Application.Abstractions;
using OumezzineAcademy.Application.UseCases;

namespace OumezzineAcademy.Tests;

public sealed class CoursePrerequisiteValidatorTests
{
    [Fact]
    public async Task Detects_direct_cycle()
    {
        var course = Guid.NewGuid();
        var prerequisite = Guid.NewGuid();
        var validator = new CoursePrerequisiteValidator(new EdgeStore(
            new(course, prerequisite), new(prerequisite, course)));

        Assert.True(await validator.WouldCreateCycleAsync(course, new[] { prerequisite }));
    }

    [Fact]
    public async Task Detects_transitive_cycle_and_ignores_duplicate_edges()
    {
        var course = Guid.NewGuid();
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        var validator = new CoursePrerequisiteValidator(new EdgeStore(
            new(first, second), new(second, course), new(first, second)));

        Assert.True(await validator.WouldCreateCycleAsync(course, new[] { first, first }));
    }

    [Fact]
    public async Task Returns_false_for_acyclic_graph()
    {
        var course = Guid.NewGuid();
        var prerequisite = Guid.NewGuid();
        var validator = new CoursePrerequisiteValidator(new EdgeStore(new CoursePrerequisiteEdge(course, prerequisite)));

        Assert.False(await validator.WouldCreateCycleAsync(course, Array.Empty<Guid>()));
    }

    private sealed class EdgeStore(params CoursePrerequisiteEdge[] edges) : ICoursePrerequisiteEdges
    {
        public Task<IReadOnlyList<CoursePrerequisiteEdge>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<CoursePrerequisiteEdge>>(edges);
    }
}
