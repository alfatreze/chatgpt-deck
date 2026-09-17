using System.Reflection;
using System.Runtime.Loader;

if (args.Length != 1)
{
    Console.Error.WriteLine("Usage: dotnet run -- <PluginApi.dll>");
    return 2;
}

var assemblyPath = Path.GetFullPath(args[0]);
var assemblyDirectory = Path.GetDirectoryName(assemblyPath)!;
AssemblyLoadContext.Default.Resolving += (_, name) =>
{
    var candidate = Path.Combine(assemblyDirectory, name.Name + ".dll");
    return File.Exists(candidate) ? AssemblyLoadContext.Default.LoadFromAssemblyPath(candidate) : null;
};
var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath);
Console.WriteLine($"Assembly: {assembly.FullName}");
Type[] exported;
try { exported = assembly.GetExportedTypes(); }
catch (ReflectionTypeLoadException error)
{
    foreach (var loaderError in error.LoaderExceptions.Where(e => e is not null)) Console.Error.WriteLine($"Loader: {loaderError!.Message}");
    exported = error.Types.Where(t => t is not null).Cast<Type>().ToArray();
}
catch (FileNotFoundException error)
{
    Console.Error.WriteLine($"Dependency missing: {error.FileName}");
    return 3;
}
foreach (var type in exported.Where(t => t.Name.Contains("BitmapImage", StringComparison.OrdinalIgnoreCase)
    || t.Name.Contains("Image", StringComparison.OrdinalIgnoreCase)
    || t.Name.Contains("ActionEditor", StringComparison.OrdinalIgnoreCase)))
{
    Console.WriteLine($"TYPE {type.FullName}");
    foreach (var constructor in type.GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)) Console.WriteLine($"  CTOR {constructor}");
    foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).OrderBy(p => p.Name)) Console.WriteLine($"  PROP {property.PropertyType} {property.Name}");
    foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly).OrderBy(m => m.Name))
        Console.WriteLine($"  {method}");
}
return 0;
