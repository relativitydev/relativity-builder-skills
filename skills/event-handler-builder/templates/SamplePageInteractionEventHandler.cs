using kCura.EventHandler;

namespace MyCompany.MyApp.EventHandlers
{
    // Page Interaction for Relativity Forms (the default for all object types).
    // The C# class only names the files; the behavior lives in JavaScript (see sample-forms-script.js).
    // Relativity Forms ignores PopulateScriptBlocks(), so return an empty Response from it.
    //
    // Upload the DLL AND every file named below as resource files on the same application.
    // Upload the .js files separately; do not embed them in the DLL.
    [kCura.EventHandler.CustomAttributes.Description("MyApp - Sample Object form script")]
    [System.Runtime.InteropServices.Guid("00000000-0000-0000-0000-000000000000")]
    public class SamplePageInteractionEventHandler : PageInteractionEventHandler
    {
        public override Response PopulateScriptBlocks()
        {
            return new Response { Success = true, Message = string.Empty };
        }

        // Files Relativity Forms loads and runs as event handler scripts.
        public override string[] ScriptFileNames => new[] { "sample-forms-script.js" };

        // Supporting files (CSS, images, extra JS) that are hosted but not run automatically.
        public override string[] AdditionalHostedFileNames => new string[0];
    }
}
