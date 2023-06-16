using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LabVIDMAutomationService.Config;
using LabVIDMAutomationService.Models;

namespace LabVIDMAutomationService.Controllers
{
    public class ContentController
    {
        public static async void PostTenantDetailsToAWContent(vIDMQueueItem queueItem)
        {
            // Generate file contents for upload
            queueItem.GenerateContentFile();
            
            // Check if existing filename from previous lab still exists and delete if it does
            bool deletedAWContent = await DeleteTenantDetailsFromAWContent(queueItem);

            try {
                bool uploadedFileToAW = await AirWatchAPIController.UploadAirWatchManagedContent(queueItem);
            }
            catch (Exception ex) {
                DatabaseController.InsertWrkshopError(ex.Message, string.Empty, ex.StackTrace);
            }
            finally
            {
                // Remove file after successful upload
                if (File.Exists(queueItem.awContentFilePath))
                    File.Delete(queueItem.awContentFilePath);
            }
        }

        public static async Task<bool> DeleteTenantDetailsFromAWContent(vIDMQueueItem queueItem)
        {
            // Check if file exists before attempting to delete
            //string existingContentID = await AirWatchAPIController.GetAirWatchManagedContentID(queueItem, queueItem.awContentFileName);
            string existingContentID = await AirWatchAPIController.GetAirWatchManagedContentID(queueItem, queueItem.awContentSearchFileName);
            if (!string.IsNullOrEmpty(existingContentID))
            {
                // ContentID found, file eixsts - remove
                return await AirWatchAPIController.DeleteAirWatchManagedContent(queueItem, existingContentID);
            }
            else
                return false;
        }

        public static async Task<bool> DeleteAllTenantDetailsFromAWContnet(vIDMQueueItem queueItem)
        {
            try
            {
                List<string> contentIDs = await AirWatchAPIController.GetAirWatchManagedContentIDs(queueItem);
                if (contentIDs != null)
                {
                    bool allDeleted = false;
                    foreach (string id in contentIDs)
                    {
                        bool deleted = await AirWatchAPIController.DeleteAirWatchManagedContent(queueItem, id);
                        if (!deleted)
                            allDeleted = false;
                    }
                    return allDeleted;
                }
                else
                    return false;
            }
            catch (Exception ex) {
                DatabaseController.InsertWrkshopError(string.Format("Could not Delete vIDM AWContent for wrkshops_id {0}!", queueItem.workshopsID), string.Empty, string.Empty);
                return false; 
            }
        }
    }
}
