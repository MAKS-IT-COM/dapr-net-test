using Core.Commands;
using MaksIT.Core.Dapr;
using MaksIT.Core.Extensions;
using Microsoft.AspNetCore.Mvc;
using Publisher.Models;
using System.Text.Json;

namespace Publisher.Controllers;
[ApiController]
[Route("[controller]")]
public class DaprTestController : ControllerBase {

  private readonly ILogger<DaprTestController> _logger;
  private readonly IDaprService _daprService;

  public DaprTestController(
    ILogger<DaprTestController> logger,
    IDaprService daprService
  ) {
    _logger = logger;
    _daprService = daprService;
  }

  [HttpPost(Name = "DaprTestPublisher")]
  public async Task<IActionResult> Post([FromBody] PostDaprTestRequest requestData) {
    var result = await _daprService.PublishEventAsync(
      "pubsub",
      "test",
      new TestCommand {
        Message = requestData.Message
      }.ToJson());
    return result.ToActionResult();
  }
}
