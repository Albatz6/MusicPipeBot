using Microsoft.Extensions.Hosting;
using MusicPipeBot;

var hostBuilder = GenericHostBuilder.GetHostBuilder();
await hostBuilder.RunConsoleAsync();