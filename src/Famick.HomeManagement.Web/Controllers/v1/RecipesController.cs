using Famick.HomeManagement.Core.DTOs.Recipes;
using Famick.HomeManagement.Core.Interfaces;
using Famick.HomeManagement.Web.Controllers;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Famick.HomeManagement.Web.Controllers.v1;

/// <summary>
/// API controller for managing recipes, ingredients, and nested recipes
/// </summary>
[ApiController]
[Route("api/v1/recipes")]
[Authorize]
public class RecipesController : ApiControllerBase
{
    private readonly IRecipeService _recipeService;
    private readonly IValidator<CreateRecipeRequest> _createValidator;
    private readonly IValidator<UpdateRecipeRequest> _updateValidator;
    private readonly IValidator<AddRecipePositionRequest> _addPositionValidator;
    private readonly IValidator<UpdateRecipePositionRequest> _updatePositionValidator;

    public RecipesController(
        IRecipeService recipeService,
        IValidator<CreateRecipeRequest> createValidator,
        IValidator<UpdateRecipeRequest> updateValidator,
        IValidator<AddRecipePositionRequest> addPositionValidator,
        IValidator<UpdateRecipePositionRequest> updatePositionValidator,
        ITenantProvider tenantProvider,
        ILogger<RecipesController> logger)
        : base(tenantProvider, logger)
    {
        _recipeService = recipeService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _addPositionValidator = addPositionValidator;
        _updatePositionValidator = updatePositionValidator;
    }

    #region Recipe CRUD

    /// <summary>
    /// Lists all recipes
    /// </summary>
    /// <param name="filter">Optional filter criteria</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of recipes (summary view)</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<RecipeSummaryDto>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> List(
        [FromQuery] RecipeFilterRequest? filter,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Listing recipes for tenant {TenantId}", TenantId);

        var recipes = await _recipeService.ListAsync(filter, cancellationToken);
        return ApiResponse(recipes);
    }

    /// <summary>
    /// Gets a specific recipe by ID
    /// </summary>
    /// <param name="id">Recipe ID</param>
    /// <param name="includePositions">Include recipe positions/ingredients (default: true)</param>
    /// <param name="includeNesting">Include nested recipes (default: true)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Recipe details with positions and nested recipes</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(RecipeDto), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetById(
        Guid id,
        [FromQuery] bool includePositions = true,
        [FromQuery] bool includeNesting = true,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting recipe {RecipeId} for tenant {TenantId}", id, TenantId);

        var recipe = await _recipeService.GetByIdAsync(id, includePositions, includeNesting, cancellationToken);

        if (recipe == null)
        {
            return NotFoundResponse($"Recipe with ID {id} not found");
        }

        return ApiResponse(recipe);
    }

    /// <summary>
    /// Creates a new recipe
    /// </summary>
    /// <param name="request">Recipe creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created recipe</returns>
    [HttpPost]
    [Authorize(Policy = "RequireEditor")]
    [ProducesResponseType(typeof(RecipeDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(409)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> Create(
        [FromBody] CreateRecipeRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ValidationErrorResponse(
                validationResult.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
            );
        }

        _logger.LogInformation("Creating recipe '{Name}' for tenant {TenantId}", request.Name, TenantId);

        var recipe = await _recipeService.CreateAsync(request, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = recipe.Id },
            recipe
        );
    }

    /// <summary>
    /// Updates an existing recipe
    /// </summary>
    /// <param name="id">Recipe ID</param>
    /// <param name="request">Recipe update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated recipe</returns>
    [HttpPut("{id}")]
    [Authorize(Policy = "RequireEditor")]
    [ProducesResponseType(typeof(RecipeDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateRecipeRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ValidationErrorResponse(
                validationResult.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
            );
        }

        _logger.LogInformation("Updating recipe {RecipeId} for tenant {TenantId}", id, TenantId);

        var recipe = await _recipeService.UpdateAsync(id, request, cancellationToken);
        return ApiResponse(recipe);
    }

    /// <summary>
    /// Deletes a recipe (soft delete)
    /// </summary>
    /// <param name="id">Recipe ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpDelete("{id}")]
    [Authorize(Policy = "RequireEditor")]
    [ProducesResponseType(204)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(422)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting recipe {RecipeId} for tenant {TenantId}", id, TenantId);

        await _recipeService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    #endregion

    #region Recipe Positions (Ingredients)

    /// <summary>
    /// Adds an ingredient/position to a recipe
    /// </summary>
    /// <param name="id">Recipe ID</param>
    /// <param name="request">Position data (product, quantity, etc.)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created recipe position</returns>
    [HttpPost("{id}/positions")]
    [Authorize(Policy = "RequireEditor")]
    [ProducesResponseType(typeof(RecipePositionDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> AddPosition(
        Guid id,
        [FromBody] AddRecipePositionRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _addPositionValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ValidationErrorResponse(
                validationResult.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
            );
        }

        _logger.LogInformation("Adding position to recipe {RecipeId} for tenant {TenantId}", id, TenantId);

        var position = await _recipeService.AddPositionAsync(id, request, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id },
            position
        );
    }

    /// <summary>
    /// Updates an existing recipe position/ingredient
    /// </summary>
    /// <param name="id">Recipe ID</param>
    /// <param name="positionId">Position ID</param>
    /// <param name="request">Position update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated recipe position</returns>
    [HttpPut("{id}/positions/{positionId}")]
    [Authorize(Policy = "RequireEditor")]
    [ProducesResponseType(typeof(RecipePositionDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> UpdatePosition(
        Guid id,
        Guid positionId,
        [FromBody] UpdateRecipePositionRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _updatePositionValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ValidationErrorResponse(
                validationResult.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
            );
        }

        _logger.LogInformation("Updating position {PositionId} in recipe {RecipeId} for tenant {TenantId}",
            positionId, id, TenantId);

        var position = await _recipeService.UpdatePositionAsync(positionId, request, cancellationToken);
        return ApiResponse(position);
    }

    /// <summary>
    /// Removes an ingredient/position from a recipe
    /// </summary>
    /// <param name="id">Recipe ID</param>
    /// <param name="positionId">Position ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpDelete("{id}/positions/{positionId}")]
    [Authorize(Policy = "RequireEditor")]
    [ProducesResponseType(204)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> RemovePosition(
        Guid id,
        Guid positionId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Removing position {PositionId} from recipe {RecipeId} for tenant {TenantId}",
            positionId, id, TenantId);

        await _recipeService.RemovePositionAsync(positionId, cancellationToken);
        return NoContent();
    }

    #endregion

    #region Recipe Nesting

    /// <summary>
    /// Adds a nested recipe (sub-recipe) to a parent recipe
    /// </summary>
    /// <param name="id">Parent recipe ID</param>
    /// <param name="childRecipeId">Child recipe ID to nest</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpPost("{id}/nested/{childRecipeId}")]
    [Authorize(Policy = "RequireEditor")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(422)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> AddNestedRecipe(
        Guid id,
        Guid childRecipeId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Adding nested recipe {ChildRecipeId} to recipe {RecipeId} for tenant {TenantId}",
            childRecipeId, id, TenantId);

        await _recipeService.AddNestedRecipeAsync(id, childRecipeId, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Removes a nested recipe from a parent recipe
    /// </summary>
    /// <param name="id">Parent recipe ID</param>
    /// <param name="childRecipeId">Child recipe ID to remove</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpDelete("{id}/nested/{childRecipeId}")]
    [Authorize(Policy = "RequireEditor")]
    [ProducesResponseType(204)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> RemoveNestedRecipe(
        Guid id,
        Guid childRecipeId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Removing nested recipe {ChildRecipeId} from recipe {RecipeId} for tenant {TenantId}",
            childRecipeId, id, TenantId);

        await _recipeService.RemoveNestedRecipeAsync(id, childRecipeId, cancellationToken);
        return NoContent();
    }

    #endregion

    #region Business Logic

    /// <summary>
    /// Checks if there is enough stock to fulfill a recipe
    /// </summary>
    /// <param name="id">Recipe ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Recipe fulfillment details with stock availability</returns>
    [HttpGet("{id}/fulfillment")]
    [ProducesResponseType(typeof(RecipeFulfillmentDto), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> CheckFulfillment(
        Guid id,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Checking stock fulfillment for recipe {RecipeId} for tenant {TenantId}", id, TenantId);

        var fulfillment = await _recipeService.CheckStockFulfillmentAsync(id, cancellationToken);
        return ApiResponse(fulfillment);
    }

    /// <summary>
    /// Gets total ingredients needed for a recipe (including nested recipes)
    /// </summary>
    /// <param name="id">Recipe ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Complete list of ingredients with aggregated quantities</returns>
    [HttpGet("{id}/ingredients")]
    [ProducesResponseType(typeof(List<IngredientRequirementDto>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetTotalIngredients(
        Guid id,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting total ingredients for recipe {RecipeId} for tenant {TenantId}", id, TenantId);

        var ingredients = await _recipeService.GetTotalIngredientsAsync(id, cancellationToken);
        return ApiResponse(ingredients);
    }

    /// <summary>
    /// Gets the complete recipe hierarchy (all nested recipes)
    /// </summary>
    /// <param name="id">Recipe ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of recipe IDs in hierarchical order</returns>
    [HttpGet("{id}/hierarchy")]
    [ProducesResponseType(typeof(List<Guid>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetHierarchy(
        Guid id,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting recipe hierarchy for recipe {RecipeId} for tenant {TenantId}", id, TenantId);

        var hierarchy = await _recipeService.GetRecipeHierarchyAsync(id, cancellationToken);
        return ApiResponse(hierarchy);
    }

    #endregion
}
