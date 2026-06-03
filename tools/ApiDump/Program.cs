using System.Reflection;
using System.Runtime.InteropServices;

// Dumps the full type/method/field surface of Il2CppBigAmbitions.dll (metadata only, no execution)
// so API discovery becomes a grep over a single text file.

string gameRoot = @"C:\Program Files (x86)\Steam\steamapps\common\Big Ambitions";
string il2cppDir = Path.Combine(gameRoot, @"MelonLoader\Il2CppAssemblies");
string net6Dir = Path.Combine(gameRoot, @"MelonLoader\net6");
string target = Path.Combine(il2cppDir, "Il2CppBigAmbitions.dll");
string outFile = args.Length > 0 ? args[0] : "Il2CppBigAmbitions.api.txt";

try
{
    var paths = new List<string>();
    if (Directory.Exists(il2cppDir)) paths.AddRange(Directory.GetFiles(il2cppDir, "*.dll"));
    if (Directory.Exists(net6Dir)) paths.AddRange(Directory.GetFiles(net6Dir, "*.dll"));
    paths.AddRange(Directory.GetFiles(RuntimeEnvironment.GetRuntimeDirectory(), "*.dll")); // real BCL core
    var resolver = new PathAssemblyResolver(paths.Distinct(StringComparer.OrdinalIgnoreCase).ToArray());

    using var mlc = new MetadataLoadContext(resolver);
    var asm = mlc.LoadFromAssemblyPath(target);

    Type?[] types;
    try { types = asm.GetTypes(); }
    catch (ReflectionTypeLoadException ex)
    {
        types = ex.Types;
        Console.Error.WriteLine($"(partial load: {ex.LoaderExceptions.Length} loader errors - using resolved types)");
    }

    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outFile)) ?? ".");
    using var w = new StreamWriter(outFile);
    int tc = 0, mc = 0, fc = 0;
    foreach (var t in types.Where(x => x != null).Cast<Type>().OrderBy(x => x.FullName, StringComparer.Ordinal))
    {
        var name = t.FullName ?? t.Name;
        if (name.Contains('<')) continue; // skip compiler-generated noise
        tc++;
        w.WriteLine($"TYPE {name} : {Safe(() => t.BaseType?.FullName) ?? "-"}");
        foreach (var f in Fields(t)) { fc++; w.WriteLine($"  F {Safe(() => f.FieldType.FullName) ?? "?"} {f.Name}"); }
        foreach (var m in Methods(t))
        {
            mc++;
            var pars = string.Join(", ", Params(m));
            w.WriteLine($"  M {(m.IsStatic ? "static " : "")}{Safe(() => m.ReturnType.FullName) ?? "?"} {m.Name}({pars})");
        }
        w.WriteLine();
    }
    Console.WriteLine($"OK types={tc} methods={mc} fields={fc} -> {Path.GetFullPath(outFile)}");
}
catch (Exception ex)
{
    Console.Error.WriteLine("DUMP FAILED: " + ex);
}

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
