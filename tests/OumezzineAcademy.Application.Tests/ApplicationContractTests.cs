using System.Collections;
using System.Reflection;
using OumezzineAcademy.Application.Abstractions;
using OumezzineAcademy.Application.Admin.Catalog;
using OumezzineAcademy.Domain.Catalog;
using Xunit;

namespace OumezzineAcademy.Tests;

public sealed class ApplicationContractTests
{
    [Fact]
    public async Task Null_cache_invalidator_completes_both_operations()
    {
        var invalidator = new NullStudyLmsCacheInvalidator();

        await invalidator.InvalidateCatalogAsync();
        await invalidator.InvalidatePublicAsync();
    }

    [Fact]
    public void Application_records_can_be_constructed_and_properties_can_be_read()
    {
        var assembly = typeof(PagedResult<>).Assembly;
        var recordTypes = assembly.GetTypes()
            .Where(type => type.Namespace == "OumezzineAcademy.Application.Abstractions"
                && type.IsClass
                && !type.IsAbstract
                && !type.IsGenericTypeDefinition
                && type.GetConstructors(BindingFlags.Public | BindingFlags.Instance).Length > 0);

        foreach (var type in recordTypes)
        {
            var constructor = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .OrderByDescending(x => x.GetParameters().Length)
                .First();
            var values = constructor.GetParameters().Select(parameter => ValueFor(parameter.ParameterType)).ToArray();
            var instance = constructor.Invoke(values);

            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                _ = property.GetValue(instance);
        }
    }

    [Fact]
    public void Paged_result_calculates_pages_and_handles_non_positive_page_size()
    {
        var paged = new PagedResult<string>(["a", "b", "c"], 2, 2, 5);
        var emptyPageSize = new PagedResult<string>([], 1, 0, 5);

        Assert.Equal(3, paged.TotalPages);
        Assert.Equal(0, emptyPageSize.TotalPages);
    }

    [Fact]
    public async Task Course_prerequisite_validation_service_delegates_to_validator()
    {
        var courseId = Guid.NewGuid();
        var expected = new[] { Guid.NewGuid() };
        var validator = new ValidatorSpy(true);
        var service = new CoursePrerequisiteValidationService(validator);

        var result = await service.WouldCreateCycleAsync(courseId, expected);

        Assert.True(result);
        Assert.Equal(courseId, validator.CourseId);
        Assert.Equal(expected, validator.Prerequisites);
    }

    private static object? ValueFor(Type type)
    {
        if (type == typeof(string)) return "value";
        if (type == typeof(Guid)) return Guid.NewGuid();
        if (type == typeof(DateTime)) return DateTime.UtcNow;
        if (type == typeof(DateTime?)) return DateTime.UtcNow;
        if (type == typeof(int)) return 1;
        if (type == typeof(bool)) return true;
        if (type.IsEnum) return Enum.GetValues(type).GetValue(0);
        if (Nullable.GetUnderlyingType(type) is { } nullable && nullable.IsEnum)
            return Enum.GetValues(nullable).GetValue(0);
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IReadOnlyList<>))
            return Array.CreateInstance(type.GetGenericArguments()[0], 0);
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>))
            return Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(type.GetGenericArguments()));
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            return Activator.CreateInstance(type);
        if (typeof(IEnumerable).IsAssignableFrom(type) && type.IsArray)
            return Array.CreateInstance(type.GetElementType()!, 0);
        return null;
    }

    private sealed class ValidatorSpy(bool result) : ICoursePrerequisiteValidator
    {
        public Guid CourseId { get; private set; }
        public IReadOnlyList<Guid> Prerequisites { get; private set; } = [];

        public Task<bool> WouldCreateCycleAsync(Guid courseId, IEnumerable<Guid> prerequisites, CancellationToken cancellationToken = default)
        {
            CourseId = courseId;
            Prerequisites = prerequisites.ToArray();
            return Task.FromResult(result);
        }
    }
}
