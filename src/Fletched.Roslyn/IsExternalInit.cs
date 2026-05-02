// Polyfill for records and init-only setters on netstandard2.0
namespace System.Runtime.CompilerServices
{
    internal sealed class IsExternalInit { }
}
