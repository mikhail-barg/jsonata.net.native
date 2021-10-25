#if !NET5_0
//see https://stackoverflow.com/a/64749403/376066
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}
#endif