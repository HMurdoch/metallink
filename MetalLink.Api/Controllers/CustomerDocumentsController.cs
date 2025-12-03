using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MetalLink.Application.Customers.Documents;
using MetalLink.Shared.Customers;

namespace MetalLink.Api.Controllers;

[ApiController]
[Route("api/customers/{customerId:long}/documents")]
[Authorize]
public sealed class CustomerDocumentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public CustomerDocumentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // Form model for uploads
    public sealed class UploadCustomerDocumentForm
    {
        public string DocumentType { get; set; } = string.Empty;
        public IFormFile File { get; set; } = default!;
    }

    // POST /api/customers/{customerId}/documents
    [HttpPost]
    [RequestSizeLimit(50 * 1024 * 1024)] // 50 MB
    [ProducesResponseType(typeof(CustomerDocumentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadDocument(
        long customerId,
        [FromForm] UploadCustomerDocumentForm form,
        CancellationToken cancellationToken)
    {
        if (form.File is null || form.File.Length == 0)
        {
            return BadRequest(new { message = "File is required." });
        }

        await using var ms = new MemoryStream();
        await form.File.CopyToAsync(ms, cancellationToken);
        var bytes = ms.ToArray();

        var command = new UploadCustomerDocumentCommand(
            CustomerId: customerId,
            DocumentType: form.DocumentType,
            FileName: form.File.FileName,
            ContentType: form.File.ContentType,
            Content: bytes
        );

        var result = await _mediator.Send(command, cancellationToken);

        return Created(string.Empty, result);
    }

    // GET /api/customers/{customerId}/documents
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CustomerDocumentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDocuments(
        long customerId,
        CancellationToken cancellationToken)
    {
        var query = new GetCustomerDocumentsQuery(customerId);
        var docs = await _mediator.Send(query, cancellationToken);
        return Ok(docs);
    }
}
