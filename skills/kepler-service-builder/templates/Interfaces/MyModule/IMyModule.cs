using Relativity.Kepler.Services;

namespace MyCompany.MyApp.Interfaces.MyModule
{
    // One module per namespace. Every service in this namespace or below belongs to it.
    // The route prefix becomes the first URL segment after /Relativity.REST/api/.
    // It must not be empty and must not contain the word "Relativity".
    [ServiceModule("My Module")]
    [RoutePrefix("my-module", VersioningStrategy.Namespace)]
    public interface IMyModule
    {
    }
}
