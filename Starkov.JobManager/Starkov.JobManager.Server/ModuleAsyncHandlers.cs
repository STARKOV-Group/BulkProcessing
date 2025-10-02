using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Starkov.JobManager.Server
{
  public partial class ModuleAsyncHandlers
  {

    public virtual void EntitiesQueueBatchProcessing(Starkov.JobManager.Server.AsyncHandlerInvokeArgs.EntitiesQueueBatchProcessingInvokeArgs args)
    {
      var logInfo = string.Format("BulkProcessing. EntitiesQueueBatchProcessing. EntitiesQueueBatchId={0}.", args.EntitiesQueueBatchId);
      Logger.DebugFormat("{0} Start.", logInfo);
      args.Retry = false;
      
      var queue = EntitiesQueueBatches.GetAll(q => q.Id == args.EntitiesQueueBatchId).FirstOrDefault();
      if (queue == null)
      {
        Logger.DebugFormat("{0} Not found EntitiesQueueBatch by Id.", logInfo);
        return;
      }
      
      if (!Locks.TryLock(queue))
      {
        Logger.DebugFormat("{0} queue is locked. retry later.", logInfo);
        return;
      }
      
      var setting = Functions.EntitiesQueueBatch.GetSetting(queue);
      if (setting == null)
      {
        Logger.DebugFormat("{0} Not found setting {1} by Id.", logInfo, queue.ProcessSetting.Id);
        return;
      }
      
      var processEntitiesResult = Structures.Module.ProcessEntitiesResult.Create();
      var processStartTime = Calendar.Now;
      
      try
      {
        queue.Iteration = queue.Iteration + 1;
        queue.ProcessingStatus = JobManager.EntitiesQueueBatch.ProcessingStatus.InProcess;
        queue.Save();
        
        logInfo += string.Format(" Setting={0} Iteration={1}.", setting.Id, queue.Iteration);
        
        var entityIds = Functions.EntitiesQueueBatch.GetEntityIds(queue);

        if (!entityIds.Any())
        {
          Logger.DebugFormat("{0} no any entity for processing.", logInfo);
          return;
        }
        
        Logger.DebugFormat("{0} ProcessEntities begin.", logInfo);
        processEntitiesResult = Functions.ProcessSettingsBase.ProcessEntities(setting, entityIds);
        Logger.DebugFormat("{0} ProcessEntities end. IsSuccess='{1}', unsuccessful count={2}.", logInfo, processEntitiesResult.IsSuccess, processEntitiesResult.UnsuccessItemsInfo?.Count);
      }
      catch (Exception ex)
      {
        Logger.ErrorFormat("{0}", ex, logInfo);
        if (!processEntitiesResult.IsSuccess.HasValue)
          processEntitiesResult.IsSuccess = false;
      }
      finally
      {
        try
        {
          processEntitiesResult.ProcessStartTime = processStartTime;
          Functions.ProcessSettingsBase.QueueResultProcessing(setting, processEntitiesResult, queue);
        }
        catch (Exception ex)
        {
          queue.ProcessingStatus = JobManager.EntitiesQueueBatch.ProcessingStatus.Error;
          Logger.ErrorFormat("{0}. QueueResultProcessing error.", ex, logInfo);
        }
        
        queue.Save();
        
        if (Locks.GetLockInfo(queue).IsLockedByMe)
          Locks.Unlock(queue);
      }
      
      // Если завершили обработку всех очередей процесса запускаем ФП для новых партий.
      if (!Functions.ProcessSettingsBase.IsInProcess(setting))
      {
        Logger.DebugFormat("{0} Done. CreateEntitiesQueueBatches Start.", logInfo);
        Jobs.CreateEntitiesQueueBatches.Enqueue();
      }
      
      Logger.DebugFormat("{0} Done.", logInfo);
    }

  }
}