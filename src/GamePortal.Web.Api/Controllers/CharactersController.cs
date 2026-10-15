using GamePortal.Application.Abstractions;
using GamePortal.Application.Characters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GamePortal.Web.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/characters")]
public sealed class CharactersController(CharacterQueryService characters, ICurrentUser currentUser) : ControllerBase
{
    /// <summary>내 캐릭터 목록. 쿠폰 보상을 받을 캐릭터를 고를 때 쓴다.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CharacterDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public Task<IReadOnlyList<CharacterDto>> GetMine(CancellationToken cancellationToken)
        => characters.GetMyCharactersAsync(currentUser.Id, cancellationToken);
}
