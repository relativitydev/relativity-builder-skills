using System;
using kCura.EventHandler;
using kCura.MassOperationHandlers;

namespace MyCompany.MyApp.EventHandlers
{
    // Mass Operation handler: adds a custom action to the mass operations menu of an object
    // type's list view. Relativity calls the methods in this order:
    //   ValidateSelection -> ValidateLayout -> PreMassOperation -> DoBatch (once per batch) -> PostMassOperation
    // The batch size comes from the MassCustomBatchAmount instance setting.
    //
    // Note: this is a different base class from object event handlers, and Relativity reads its
    // display name from System.ComponentModel.Description, NOT the kCura.EventHandler attribute
    // (with the kCura one, the handler registers with a blank description).
    // Attach it from the object type's Mass Operations list (or link it on the application).
    [System.ComponentModel.Description("MyApp - Sample mass action")]
    [System.Runtime.InteropServices.Guid("00000000-0000-0000-0000-000000000000")]
    public class SampleMassOperationHandler : MassOperationHandler
    {
        public override Response ValidateSelection()
        {
            // Check the selected items (Artifact IDs are in MassActionTableName) before anything runs.
            return Ok();
        }

        public override Response ValidateLayout()
        {
            // Only relevant when the mass operation uses a layout (LayoutMask). Return Ok otherwise.
            return Ok();
        }

        public override Response PreMassOperation()
        {
            return Ok();
        }

        public override Response DoBatch()
        {
            try
            {
                foreach (int artifactId in BatchIDs)
                {
                    if (IsCancelled())
                    {
                        break;
                    }

                    // ... process artifactId (keep each batch short) ...
                }

                IncrementCount(BatchIDs.Count);
                return Ok();
            }
            catch (Exception ex)
            {
                return new Response { Success = false, Message = ex.Message, Exception = ex };
            }
        }

        public override Response PostMassOperation()
        {
            return Ok();
        }

        private static Response Ok()
        {
            return new Response { Success = true, Message = string.Empty };
        }
    }
}
