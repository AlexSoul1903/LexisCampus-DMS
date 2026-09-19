using LexisCampusDMS.Application.Common;
using LexisCampusDMS.Application.DTOs;
using LexisCampusDMS.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Memory;

namespace LexisCampusDMS.Server.Controllers;

/// <summary>
/// Public verification controller enabling external entities (employers, academic institutions, certifiers)
/// to validate document authenticity, issuing dean, and integrity via cryptographic SHA-256 hash or token.
/// Protected with rate limiting and optimized with in-memory caching.
/// </summary>
[ApiController]
[Route("api/public")]
[AllowAnonymous]
[EnableRateLimiting("PublicVerificationRateLimit")]
public class PublicVerificationController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<PublicVerificationController> _logger;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    public PublicVerificationController(
        IDocumentService documentService,
        IMemoryCache memoryCache,
        ILogger<PublicVerificationController> logger)
    {
        _documentService = documentService ?? throw new ArgumentNullException(nameof(documentService));
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Validates the authenticity of an academic document by its cryptographic SHA-256 hash or unique ID token.
    /// Accessible publicly without authentication. Results are cached in memory for high performance.
    /// </summary>
    /// <param name="hashOrToken">64-character SHA-256 file hash or unique document token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Document authenticity details retrieved successfully.</response>
    /// <response code="400">Invalid or empty hash or token provided.</response>
    /// <response code="404">No institutional document corresponds to the provided hash or token.</response>
    /// <response code="429">Rate limit exceeded. Too many requests.</response>
    [HttpGet("verify/{hashOrToken}")]
    [ProducesResponseType(typeof(Result<PublicVerificationResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<PublicVerificationResponseDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<PublicVerificationResponseDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Verify(
        string hashOrToken, 
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(hashOrToken))
        {
            return BadRequest(Result<PublicVerificationResponseDto>.Failure(
                "El hash o token del documento es requerido.",
                "INVALID_INPUT"));
        }

        var normalizedKey = hashOrToken.Trim().ToLowerInvariant();
        var cacheKey = $"pub_verify_{normalizedKey}";

        // 1. Check in-memory cache for high-traffic scalability
        if (_memoryCache.TryGetValue(cacheKey, out Result<PublicVerificationResponseDto>? cachedResult) && cachedResult is not null)
        {
            Response.Headers["X-Cache"] = "HIT";
            Response.Headers.CacheControl = "public, max-age=600";
            _logger.LogDebug("Cache HIT for public verification of hash/token '{Token}'", normalizedKey);
            return Ok(cachedResult);
        }

        Response.Headers["X-Cache"] = "MISS";
        Response.Headers.CacheControl = "public, max-age=600";

        // 2. Query application service
        var result = await _documentService.VerifyPublicDocumentAsync(normalizedKey, cancellationToken);
        if (!result.IsSuccess)
        {
            return result.ErrorCode == "DOCUMENT_NOT_FOUND" 
                ? NotFound(result) 
                : BadRequest(result);
        }

        // 3. Cache valid result in memory
        _memoryCache.Set(cacheKey, result, CacheDuration);
        _logger.LogInformation("Document '{Title}' successfully verified publicly via hash/token '{Token}'", 
            result.Data?.Title, normalizedKey);

        return Ok(result);
    }
}
