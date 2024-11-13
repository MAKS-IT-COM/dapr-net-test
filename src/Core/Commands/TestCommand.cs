using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Commands;
public class TestCommand {
  [Required]
  public required string Message { get; set; }
}
