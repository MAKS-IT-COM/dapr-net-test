using Core.Commands;
using Dapr;
using MaksIT.Core.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Subscriber.Controllers;
[ApiController]
[Route("[controller]")]
public class DaprTestController : ControllerBase {
  
  private readonly ILogger<DaprTestController> _logger;

  public DaprTestController(
    ILogger<DaprTestController> logger
  ) {
    _logger = logger;
  }

  [Topic("pubsub", "test")]
  [HttpPost(Name = "DaprTestSubscriber")]
  public IActionResult Post([FromBody] string json) {

    var requestData = json.ToObject<TestCommand>();
    if (requestData == null)
      return BadRequest();

    _logger.LogInformation("Received message: {0}", requestData.Message);
    return Ok();
  }
}
