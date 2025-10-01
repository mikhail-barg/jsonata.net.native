using Jsonata.Net.Native.Json;

namespace Jsonata.Net.Native
{
    /// <summary>
    /// Provides access to JSONata bindings (variables and functions) during evaluation.
    /// Custom built-in functions can receive this interface via the
    /// [BindingLookupArgument] attribute to access bindings in the evaluation environment.
    /// </summary>
    public interface IBindingLookup
    {
        /// <summary>
        /// Looks up a binding by name in the evaluation environment.
        /// Searches the current environment and parent environments
        /// hierarchically until found or all environments exhausted.
        /// Can return variables, functions, or any bound JToken value.
        /// </summary>
        /// <param name="name">The binding name to look up (without '$' prefix for variables/functions)</param>
        /// <returns>
        /// The JToken value bound to the name (can be a value, function, or UNDEFINED if not found)
        /// </returns>
        JToken Lookup(string name);
    }
}
