using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Starkov.JobManager.Server
{
  public partial class ModuleJobs
  {

    /// <summary>
    /// Создать очередь обработки
    /// </summary>
    public virtual void CreateEntitiesQueueBatches()
    {
      var logInfo = "BulkProcessing. CreateEntitiesQueueBatches.";
      
      var settings = ProcessSettingsBases.GetAll()
        .Where(_ => _.Status == JobManager.ProcessSettingsBase.Status.Active)
        .Where(_ => _.ProcessStatus == JobManager.ProcessSettingsBase.ProcessStatus.InProcess)
        .OrderByDescending(_ => _.Priority);
      
      var totalMaxFlowCount = Functions.Module.GetTotalFlowCount();
      var reservedFlowCount = Functions.Module.GetQueueBatchesInProcess().Count();
      
      foreach (var setting in settings)
      {
        if (reservedFlowCount >= totalMaxFlowCount)
        {
          Logger.DebugFormat("{0} The maximum value of allowed streams ({1}) has been reached.", logInfo, totalMaxFlowCount);
          break;
        }
        
        CleanStalledQueues(setting);
        
        // Не берем процесс в работу, пока для него есть обрабатывамые очереди во избежание дублирования данных.
        if (Functions.ProcessSettingsBase.GetProcessSettingQueue(setting)
            .Where(_ => _.ProcessingStatus == JobManager.EntitiesQueueBatch.ProcessingStatus.InProcess ||
                   _.ProcessingStatus == JobManager.EntitiesQueueBatch.ProcessingStatus.Scheduled)
            .Any())
        {
          continue;
        }
        
        Logger.DebugFormat("{0} Processing setting={1} «{2}». Flow Limit={3}, Total Flow Limit={4}", logInfo, setting.Id, setting.Name, setting.FlowCount, totalMaxFlowCount);
        
        try
        {
          var totalQueryRange = Functions.ProcessSettingsBase.GetEntitiesIdsConsideringExceptions(setting);
          
          var skip = 0;
          var batchSize = Functions.ProcessSettingsBase.GetBatchSize(setting);
          
          // Ограничение выборки по настройкам потоков.
          var availableFlowCount = totalMaxFlowCount - reservedFlowCount;
          var limit = PublicFunctions.ProcessSettingsBase.GetSettingFlowLimit(setting, availableFlowCount);
          
          for (int queueIndex = 0; queueIndex < limit; queueIndex++)
          {
            if (reservedFlowCount >= totalMaxFlowCount)
            {
              Logger.DebugFormat("{0} The maximum value of allowed streams ({1}) has been reached.", logInfo, totalMaxFlowCount);
              break;
            }
            
            var range = totalQueryRange
              .Skip(skip)
              .Take(batchSize)
              .ToList();
            
            if (!range.Any())
              break;
            
            Functions.ProcessSettingsBase.CreateEntitiesQueueBatch(setting, range);
            
            skip += batchSize;
            reservedFlowCount++;
          }
        }
        catch (Exception ex)
        {
          Logger.ErrorFormat("{0} Break processing setting={1}.", ex, logInfo, setting.Id);
          return;
        }
      }
      
      // Запустить ФП для асинхронной обработки очередей.
      Jobs.CreateAsyncForEntitiesBatch.Enqueue();
    }
    
    /// <summary>
    /// Создать асинхронный обработчик для обработки партии объектов.
    /// </summary>
    public virtual void CreateAsyncForEntitiesBatch()
    {
      var logInfo = "BulkProcessing. CreateAsyncForEntitiesBatch.";
      
      var totalMaxFlowCount = Functions.Module.GetTotalFlowCount();
      var reservedFlowCount = Functions.Module.GetQueueBatchesInProcess().Count();
      
      if (reservedFlowCount >= totalMaxFlowCount)
      {
        Logger.DebugFormat("{0} The maximum value of allowed streams ({1}) has been reached.", logInfo, totalMaxFlowCount);
        return;
      }
      
      var settings = ProcessSettingsBases.GetAll()
        .Where(_ => _.Status == JobManager.ProcessSettingsBase.Status.Active)
        .Where(_ => _.ProcessStatus == JobManager.ProcessSettingsBase.ProcessStatus.InProcess)
        .OrderByDescending(_ => _.Priority);
      
      foreach (var setting in settings)
      {
        if (reservedFlowCount >= totalMaxFlowCount)
        {
          Logger.DebugFormat("{0} The maximum value of allowed streams ({1}) has been reached.", logInfo, totalMaxFlowCount);
          break;
        }
        
        var retryTime = Calendar.Now.AddMinutes(0 - setting.RetryInterval.GetValueOrDefault());
        
        // Ограничение выборки по настройкам потоков.
        var availableFlowCount = totalMaxFlowCount - reservedFlowCount;
        var limit = PublicFunctions.ProcessSettingsBase.GetSettingFlowLimit(setting, availableFlowCount);
        
        var pendingBatches = Functions.ProcessSettingsBase.GetProcessSettingQueue(setting)
          .Where(_ => _.ProcessingStatus == JobManager.EntitiesQueueBatch.ProcessingStatus.Scheduled ||
                 _.ProcessingStatus == JobManager.EntitiesQueueBatch.ProcessingStatus.RetryWaiting && _.Modified < retryTime)
          .OrderBy(_ => _.Id)
          .Take(limit);
        
        if (!pendingBatches.Any())
        {
          Logger.DebugFormat("{0} no queues available for setting={1}.", logInfo, setting.Id);
          continue;
        }
        
        foreach (var queue in pendingBatches)
        {
          var queueBatchProcessing = AsyncHandlers.EntitiesQueueBatchProcessing.Create();
          queueBatchProcessing.EntitiesQueueBatchId = queue.Id;
          queueBatchProcessing.ExecuteAsync();
          reservedFlowCount++;
        }
      }
    }
    
    /// <summary>
    /// Очистить зависшие задачи обработки
    /// </summary>
    private static void CleanStalledQueues(IProcessSettingsBase setting)
    {
      var staleThresholdTime = Calendar.Now.AddHours(-2);
      
      var stalledQueues = Functions.ProcessSettingsBase.GetProcessSettingQueue(setting)
        .Where(_ => _.ProcessingStatus == JobManager.EntitiesQueueBatch.ProcessingStatus.InProcess)
        .Where(_ => _.Modified < staleThresholdTime);
      
      foreach (var queue in stalledQueues)
      {
        try
        {
          JobManager.EntitiesQueueBatches.Delete(queue);
        }
        catch (Exception ex)
        {
          Logger.ErrorFormat("Failed to delete stalled queue {0}. Error: {1}",
                             queue.Id, ex.Message);
        }
      }
    }

  }
}