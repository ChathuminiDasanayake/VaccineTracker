using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VaccineTracker.API.Authorization;
using VaccineTracker.Application.Interfaces;
using VaccineTracker.Contracts.Common;
using VaccineTracker.Contracts.VaccineProducts;

namespace VaccineTracker.API.Controllers;

[ApiController]
[Authorize]
[Route("api/vaccine-products")]
public sealed class VaccineProductsController : ControllerBase
{
    private readonly IVaccineProductsService _productsService;

    public VaccineProductsController(IVaccineProductsService productsService)
    {
        _productsService = productsService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<VaccineProductResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResponse<VaccineProductResponse>>> GetProducts(
        [FromQuery] GetVaccineProductsRequest request,
        CancellationToken cancellationToken)
    {
        var products = await _productsService.GetProductsAsync(
            request,
            cancellationToken);

        return Ok(products);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(VaccineProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VaccineProductResponse>> GetProduct(
        Guid id,
        CancellationToken cancellationToken)
    {
        var product = await _productsService.GetProductAsync(
            id,
            cancellationToken);

        return Ok(product);
    }

    [Authorize(Policy = AuthorizationPolicies.PlatformAdmin)]
    [HttpPost]
    [ProducesResponseType(typeof(VaccineProductResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<VaccineProductResponse>> CreateProduct(
        CreateVaccineProductRequest request,
        CancellationToken cancellationToken)
    {
        var product = await _productsService.CreateProductAsync(
            request,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetProduct),
            new { id = product.Id },
            product);
    }

    [Authorize(Policy = AuthorizationPolicies.PlatformAdmin)]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(VaccineProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<VaccineProductResponse>> UpdateProduct(
        Guid id,
        UpdateVaccineProductRequest request,
        CancellationToken cancellationToken)
    {
        var product = await _productsService.UpdateProductAsync(
            id,
            request,
            cancellationToken);

        return Ok(product);
    }

    [Authorize(Policy = AuthorizationPolicies.PlatformAdmin)]
    [HttpPatch("{id:guid}/activate")]
    [ProducesResponseType(typeof(VaccineProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VaccineProductResponse>> ActivateProduct(
        Guid id,
        CancellationToken cancellationToken)
    {
        var product = await _productsService.ActivateProductAsync(
            id,
            cancellationToken);

        return Ok(product);
    }

    [Authorize(Policy = AuthorizationPolicies.PlatformAdmin)]
    [HttpPatch("{id:guid}/deactivate")]
    [ProducesResponseType(typeof(VaccineProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VaccineProductResponse>> DeactivateProduct(
        Guid id,
        CancellationToken cancellationToken)
    {
        var product = await _productsService.DeactivateProductAsync(
            id,
            cancellationToken);

        return Ok(product);
    }
}
