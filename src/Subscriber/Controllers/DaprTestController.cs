using Core;
using Core.Commands;
using Core.StateEntities;
using Dapr;
using MaksIT.Dapr;
using MaksIT.Core.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Subscriber.Controllers;
[ApiController]
[Route("[controller]")]
public class DaprTestController : ControllerBase {
  
  private readonly ILogger<DaprTestController> _logger;
  private readonly IDaprStateStoreService _daprStateStoreService;

  public DaprTestController(
    ILogger<DaprTestController> logger,
    IDaprStateStoreService daprStateStoreService
  ) {
    _logger = logger;
    _daprStateStoreService = daprStateStoreService;
  }

  [Topic(DaprPubSubComponents.PubSub, DaprPubSubTopics.Test)]
  [HttpPost(Name = "DaprTestSubscriber")]
  public async Task<IActionResult> Post([FromBody] string json) {

    var requestData = json.ToObject<TestCommand>();
    if (requestData == null)
      return BadRequest();

    _logger.LogInformation($"Pubsub command message: {requestData.Message}");

    var getStateResult = await _daprStateStoreService.GetStateAsync<TestStateEntity>(DaprStateStoreComponents.SharedStore, DaprShardStateStoreKeys.Test);
    if (!getStateResult.IsSuccess || getStateResult.Value == null) {
      return getStateResult.ToActionResult();
    }

    var state = getStateResult.Value;
    _logger.LogInformation($"Shared state message: {state.Message}");

    var deleteStateResult = await _daprStateStoreService.DeleteStateAsync(DaprStateStoreComponents.SharedStore, DaprShardStateStoreKeys.Test);
    if (!deleteStateResult.IsSuccess)
      return deleteStateResult.ToActionResult();

    return Ok();
  }
}
