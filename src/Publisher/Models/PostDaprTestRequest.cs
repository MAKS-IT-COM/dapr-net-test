using MaksIT.Core.Abstractions.Webapi;
using System.ComponentModel.DataAnnotations;

namespace Publisher.Models;

public class PostDaprTestRequest : RequestModelBase {
  [Required]
  public required string Message { get; set; }
}
