using System;
using System.ComponentModel;
using ModelContextProtocol.Server;

namespace ServerMCP.McpTools;

[McpServerToolType]
public sealed class GreetingTool {
  public GreetingTool() { }
  
  [McpServerTool, Description("Says Hello to a user")]
  public static string Echo(string username) {
    return "Hello " + username;
  }
}