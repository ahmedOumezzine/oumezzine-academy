using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Logging;
using OumezzineAcademy.Application.Abstractions;

namespace OumezzineAcademy.Web.Services.Caching;

public sealed class StudyLmsCacheInvalidator : IStudyLmsCacheInvalidator
{
    private readonly IOutputCacheStore _outputCache;
    private readonly ILogger<StudyLmsCacheInvalidator> _logger;

    public StudyLmsCacheInvalidator(IOutputCacheStore outputCache, ILogger<StudyLmsCacheInvalidator> logger)
    {
        _outputCache = outputCache;
        _logger = logger;
    }

    public async Task InvalidateCatalogAsync(CancellationToken token = default)
    {
        await _outputCache.EvictByTagAsync(StudyLmsCacheKeys.PublicTag, token);
        _logger.LogDebug("LMS catalog output cache invalidated.");
    }

    public async Task InvalidatePublicAsync(CancellationToken token = default)
    {
        await InvalidateCatalogAsync(token);
    }
}

