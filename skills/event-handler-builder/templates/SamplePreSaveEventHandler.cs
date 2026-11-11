using System;
using System.Linq;
using kCura.EventHandler;

namespace MyCompany.MyApp.EventHandlers
{
    // Pre Save: runs before a Document or RDO is written. Use it to validate or adjust values.
    // Returning Success = false cancels the save and shows Message to the user.
    // Keep it fast (well under a second). Move slow work to an agent.
    [kCura.EventHandler.CustomAttributes.Description("MyApp - Validate Sample Object before save")]
    [System.Runtime.InteropServices.Guid("00000000-0000-0000-0000-000000000000")]
    public class SamplePreSaveEventHandler : PreSaveEventHandler
    {
        // Reference fields by GUID, not by name or Artifact ID. GUIDs stay the same in every
        // workspace the application is installed into; names and Artifact IDs do not.
        private static readonly Guid DueDateFieldGuid = new Guid("00000000-0000-0000-0000-000000000000");

        public override Response Execute()
        {
            var response = new Response { Success = true, Message = string.Empty };

            try
            {
                Field dueDate = FindField(DueDateFieldGuid);

                if (dueDate != null && !dueDate.Value.IsNull
                    && dueDate.Value.Value is DateTime due && due.Date < DateTime.UtcNow.Date)
                {
                    response.Success = false;
                    response.Message = "Due Date cannot be in the past.";
                }
            }
            catch (Exception ex)
            {
                Helper.GetLoggerFactory().GetLogger().ForContext<SamplePreSaveEventHandler>()
                    .LogError(ex, "Pre Save validation failed for ArtifactID {ArtifactID}.", ActiveArtifact.ArtifactID);
                response.Success = false;
                response.Message = "Validation could not be completed. Contact your administrator.";
                response.Exception = ex;
            }

            return response;
        }

        // ActiveArtifact.Fields only holds fields on the current layout plus the fields listed
        // here. List every field Execute reads so it works from any layout and from API saves.
        public override FieldCollection RequiredFields
        {
            get
            {
                var fields = new FieldCollection();
                fields.Add(new Field(DueDateFieldGuid));
                return fields;
            }
        }

        private Field FindField(Guid fieldGuid)
        {
            return ActiveArtifact.Fields
                .Cast<Field>()
                .FirstOrDefault(field => field.Guids != null && field.Guids.Contains(fieldGuid));
        }
    }
}
