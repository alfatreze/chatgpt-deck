# SDK API inspector

Inspect the public image-related API exposed by an installed `PluginApi.dll`:

```sh
dotnet run --project tools/ApiInspector/ApiInspector.csproj -- /path/to/PluginApi.dll
```

Run it from a directory containing the host’s matching managed dependencies. If type loading reports a missing assembly (for example `SkiaSharp`), use the host/plugin-service assembly directory rather than the development package, which may contain a compatibility shim.
