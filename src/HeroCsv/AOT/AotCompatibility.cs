using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace HeroCsv.AOT;

/// <summary>
/// AOT compatibility attributes and helpers for HeroCsv
/// </summary>
internal static class AotCompatibility
{
    /// <summary>
    /// Marks that this library is AOT compatible
    /// </summary>
    internal const bool IsAotCompatible = true;
    
    /// <summary>
    /// Ensures type is preserved for AOT compilation
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [RequiresUnreferencedCode("This method requires dynamic type access")]
    internal static void PreserveType<T>()
    {
        // This method exists to preserve type information for AOT
        _ = typeof(T);
    }
    
    /// <summary>
    /// Marks method as AOT-safe (no reflection or dynamic code)
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Class, Inherited = false)]
    internal sealed class AotSafeAttribute : System.Attribute
    {
        public AotSafeAttribute(string reason = "") { }
    }
    
    /// <summary>
    /// Marks method as requiring runtime code generation
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Class, Inherited = false)]
    internal sealed class RequiresRuntimeCodeGenerationAttribute : System.Attribute
    {
        public RequiresRuntimeCodeGenerationAttribute(string reason) { }
    }
}