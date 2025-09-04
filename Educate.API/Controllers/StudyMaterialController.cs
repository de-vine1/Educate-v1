using System.Security.Claims;
using Educate.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Educate.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StudyMaterialController : ControllerBase
{
    private readonly IStudyMaterialService _studyMaterialService;

    public StudyMaterialController(IStudyMaterialService studyMaterialService)
    {
        _studyMaterialService = studyMaterialService;
    }

    [HttpGet("subject/{subjectId}")]
    public async Task<IActionResult> GetMaterialsBySubject(int subjectId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
            return Unauthorized();

        var materials = await _studyMaterialService.GetMaterialsBySubjectAsync(subjectId, userId);
        return Ok(materials);
    }

    [HttpGet("{materialId}")]
    public async Task<IActionResult> GetMaterial(int materialId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
            return Unauthorized();

        var material = await _studyMaterialService.GetMaterialAsync(materialId, userId);
        if (material == null)
            return NotFound("Material not found or access denied");

        return Ok(material);
    }

    [HttpGet("{materialId}/access")]
    public async Task<IActionResult> CheckMaterialAccess(int materialId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
            return Unauthorized();

        var hasAccess = await _studyMaterialService.CanAccessMaterialAsync(materialId, userId);
        return Ok(new { hasAccess });
    }
}
