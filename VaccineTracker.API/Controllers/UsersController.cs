using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VaccineTracker.API.Authorization;
using VaccineTracker.Application.Interfaces;
using VaccineTracker.Application.Users;
using VaccineTracker.Contracts.Users;

namespace VaccineTracker.API.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.HospitalAdmin)]
[Route("api/[controller]")]
public sealed class UsersController : ControllerBase
{
    private readonly IUsersService _usersService;

    public UsersController(IUsersService usersService)
    {
        _usersService = usersService;
    }

    [HttpPost("hospital-users")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserResponse>> CreateHospitalUser(
        CreateHospitalUserRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _usersService.CreateHospitalUserAsync(request, cancellationToken);

        return ToActionResult(
            result,
            user => CreatedAtAction(nameof(GetUser), new { id = user.Id }, user));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<UserResponse>> GetUser(Guid id, CancellationToken cancellationToken)
    {
        var result = await _usersService.GetUserAsync(id, cancellationToken);

        return ToActionResult(result, user => Ok(user));
    }

    [HttpPatch("{id:guid}/role")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> AssignRole(
        Guid id,
        AssignUserRoleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _usersService.AssignRoleAsync(id, request, cancellationToken);

        return ToActionResult(result, user => Ok(user));
    }

    [HttpPatch("{id:guid}/activate")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> ActivateUser(Guid id, CancellationToken cancellationToken)
    {
        var result = await _usersService.ActivateUserAsync(id, cancellationToken);

        return ToActionResult(result, user => Ok(user));
    }

    [HttpPatch("{id:guid}/deactivate")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> DeactivateUser(Guid id, CancellationToken cancellationToken)
    {
        var result = await _usersService.DeactivateUserAsync(id, cancellationToken);

        return ToActionResult(result, user => Ok(user));
    }

    private ActionResult<UserResponse> ToActionResult(
        UserOperationResult<UserResponse> result,
        Func<UserResponse, ActionResult<UserResponse>> onSuccess)
    {
        return result.Status switch
        {
            UserOperationStatus.Success when result.Value is not null => onSuccess(result.Value),
            UserOperationStatus.Unauthorized => Unauthorized(),
            UserOperationStatus.NotFound => NotFound(),
            UserOperationStatus.Forbidden => Forbid(),
            UserOperationStatus.Conflict => Conflict("A user with the same username or email already exists."),
            UserOperationStatus.InvalidRole => BadRequest("The requested role cannot be assigned by the current user."),
            UserOperationStatus.InvalidGender => BadRequest("The requested gender is invalid."),
            UserOperationStatus.InvalidHospital => BadRequest("A valid active hospital is required."),
            _ => BadRequest()
        };
    }
}
