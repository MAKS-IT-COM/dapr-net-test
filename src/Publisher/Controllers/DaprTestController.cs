using Core;
using Core.Commands;
using Core.StateEntities;
using MaksIT.Dapr;
using MaksIT.Core.Extensions;
using Microsoft.AspNetCore.Mvc;
using Publisher.Models;

namespace Publisher.Controllers;
[ApiController]
[Route("[controller]")]
public class DaprTestController : ControllerBase {

  private readonly ILogger<DaprTestController> _logger;
  private readonly IDaprPublisherService _daprPublisherService;
  private readonly IDaprStateStoreService _daprStateStoreService;

  public DaprTestController(
    ILogger<DaprTestController> logger,
    IDaprPublisherService daprPublisherService,
    IDaprStateStoreService daprStateStoreService
  ) {
    _logger = logger;
    _daprPublisherService = daprPublisherService;
    _daprStateStoreService = daprStateStoreService;
  }

  [HttpPost(Name = "DaprTestPublisher")]
  public async Task<IActionResult> Post([FromBody] PostDaprTestRequest requestData) {

    var setStateResult = await _daprStateStoreService.SetStateAsync(DaprStateStoreComponents.SharedStore, DaprShardStateStoreKeys.Test, new TestStateEntity {
      Message = requestData.Message
    });

    if (!setStateResult.IsSuccess)
      return setStateResult.ToActionResult();

    var gerStateResult = await _daprStateStoreService.GetStateAsync<TestStateEntity>(DaprStateStoreComponents.SharedStore, DaprShardStateStoreKeys.Test);
    if (!gerStateResult.IsSuccess || gerStateResult.Value == null) {
      return gerStateResult.ToActionResult();
    }

    var state = gerStateResult.Value;

    _logger.LogInformation($"Shared state message: {state.Message}");

    var result = await _daprPublisherService.PublishEventAsync(
      DaprPubSubComponents.PubSub,
      DaprPubSubTopics.Test,
      new TestCommand {
        Message = requestData.Message
      }.ToJson());

    return result.ToActionResult();
  }
}
