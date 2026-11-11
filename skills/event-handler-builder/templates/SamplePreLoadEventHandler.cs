using System;
using kCura.EventHandler;

namespace MyCompany.MyApp.EventHandlers
{
    // Pre Load: runs on the server before a layout is shown in View or Edit mode.
    // Use it to set default or computed values on the form.
    //
    // Relativity Forms (the default for all object types) does NOT run Pre Load handlers when a
    // NEW object is being created. For defaults on new objects, handle
    // eventNames.REPLACE_GET_NEW_OBJECT_INSTANCE in a Page Interaction (Relativity Forms) script.
    [kCura.EventHandler.CustomAttributes.Description("MyApp - Default values on Sample Object layout")]
    [System.Runtime.InteropServices.Guid("00000000-0000-0000-0000-000000000000")]
    public class SamplePreLoadEventHandler : PreLoadEventHandler
    {
        public override Response Execute()
        {
            var response = new Response { Success = true, Message = string.Empty };

            try
            {
                if (PageMode == kCura.EventHandler.Helper.PageMode.Edit)
                {
                    // Read or set values on ActiveArtifact.Fields here. Only fields on the layout
                    // or listed in RequiredFields are present.
                }
            }
            catch (Exception ex)
            {
                Helper.GetLoggerFactory().GetLogger().ForContext<SamplePreLoadEventHandler>()
                    .LogError(ex, "Pre Load failed for ArtifactID {ArtifactID}.", ActiveArtifact.ArtifactID);
                response.Success = false;
                response.Message = "The page could not be prepared. Contact your administrator.";
                response.Exception = ex;
            }

            return response;
        }

        public override FieldCollection RequiredFields => new FieldCollection();
    }
}
