using System.Reflection;
using System.Runtime.InteropServices;

// Dumps the public+nonpublic type/method/field surface of the game's managed assemblies (metadata only,
// no execution) so API discovery is a grep over text. Works on the EA 0.11 Mono build.
//   args: <managedDir> <outFile> [namePrefix=BigAmbitions]
// Example: ApiDump "C:\...\Big Ambitions_Data\Managed" out.txt BigAmbitions

string managedDir = args.Length > 0 ? args[0] : @"C:\Program Files (x86)\Steam\steamapps\common\Big Ambitions\Big Ambitions_Data\Managed";
string outFile = args.Length > 1 ? args[1] : "BigAmbitions.api.txt";
string prefix = args.Length > 2 ? args[2] : "BigAmbitions";

try
{
    // The Mono Managed folder is self-contained (its own mscorlib/netstandard/UnityEngine/etc.).
    // Use ONLY it as the resolver, with Mono's mscorlib as the core assembly — mixing in the host
    // .NET runtime DLLs causes core-assembly conflicts.
    var resolverPaths = Directory.GetFiles(managedDir, "*.dll");
    var resolver = new PathAssemblyResolver(resolverPaths);

    string core = File.Exists(Path.Combine(managedDir, "mscorlib.dll")) ? "mscorlib"
                 : File.Exists(Path.Combine(managedDir, "netstandard.dll")) ? "netstandard"
                 : "System.Private.CoreLib";
    using var mlc = new MetadataLoadContext(resolver, core);

    var targets = Directory.GetFiles(managedDir, prefix + "*.dll")
        .Where(p => !Path.GetFileName(p).Contains("ModAPI") && !Path.GetFileName(p).Contains("ModsInternal"))
        .OrderBy(p => p, StringComparer.Ordinal)
        .ToList();

    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outFile)) ?? ".");
    using var w = new StreamWriter(outFile);
    int tc = 0, mc = 0, fc = 0;

    foreach (var dll in targets)
    {
        Assembly asm;
        try { asm = mlc.LoadFromAssemblyPath(dll); }
        catch (Exception ex) { w.WriteLine($"# FAILED to load {Path.GetFileName(dll)}: {ex.Message}"); continue; }

        Type?[] types;
        try { types = asm.GetTypes(); }
        catch (ReflectionTypeLoadException ex) { types = ex.Types; }

        string asmName = Path.GetFileName(dll);
        foreach (var t in types.Where(x => x != null).Cast<Type>().OrderBy(x => x.FullName, StringComparer.Ordinal))
        {
            var name = t.FullName ?? t.Name;
            if (name.Contains('<')) continue; // compiler-generated noise
            tc++;
            w.WriteLine($"TYPE [{asmName}] {name} : {Safe(() => t.BaseType?.FullName) ?? "-"}");
            foreach (var f in Fields(t)) { fc++; w.WriteLine($"  F {Safe(() => f.FieldType.FullName) ?? "?"} {f.Name}"); }
            foreach (var m in Methods(t))
            {
                mc++;
                var pars = string.Join(", ", Params(m));
                w.WriteLine($"  M {(m.IsStatic ? "static " : "")}{(m.IsPublic ? "" : "(np) ")}{Safe(() => m.ReturnType.FullName) ?? "?"} {m.Name}({pars})");
            }
            w.WriteLine();
        }
    }
    Console.WriteLine($"OK assemblies={targets.Count} types={tc} methods={mc} fields={fc} -> {Path.GetFullPath(outFile)}");
}
catch (Exception ex) { Console.Error.WriteLine("DUMP FAILED: " + ex); }

static IEnumerable<FieldInfo> Fields(Type t)
{
    try { return t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly); }
    catch { return Array.Empty<FieldInfo>(); }
}
static IEnumerable<MethodInfo> Methods(Type t)
{
    try { return t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly); }
    catch { return Array.Empty<MethodInfo>(); }
}
static IEnumerable<string> Params(MethodInfo m)
{
    ParameterInfo[] ps;
    try { ps = m.GetParameters(); } catch { return new[] { "?" }; }
    return ps.Select(p => (Safe(() => p.ParameterType.FullName) ?? "?") + " " + p.Name);
}
static string? Safe(Func<string?> f) { try { return f(); } catch { return null; } }
