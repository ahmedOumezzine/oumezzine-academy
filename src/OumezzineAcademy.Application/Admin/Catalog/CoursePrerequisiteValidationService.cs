using OumezzineAcademy.Application.Abstractions;

namespace OumezzineAcademy.Application.Admin.Catalog;

public sealed class CoursePrerequisiteValidationService(ICoursePrerequisiteValidator validator) : ICoursePrerequisiteValidator


{ public Task<bool> WouldCreateCycleAsync(Guid courseId, IEnumerable<Guid> prerequisites, CancellationToken token = default) => validator.WouldCreateCycleAsync(courseId, prerequisites, token); }