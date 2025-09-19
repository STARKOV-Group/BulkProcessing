using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Starkov.JobManager.ProcessSettingsBase;

namespace Starkov.JobManager.Server
{
  partial class ProcessSettingsBaseFunctions
  {

    /// <summary>
    /// Получить список ошибок.
    /// </summary>
    /// <returns></returns>
    [Remote]
    public virtual IQueryable<IEntitiesQueueBatch> GetQueueErrors()
    {
      return GetProcessSettingQueue()
        .Where(q => q.ProcessingStatus == JobManager.EntitiesQueueBatch.ProcessingStatus.Error
               || q.ProcessingStatus == JobManager.EntitiesQueueBatch.ProcessingStatus.RetryWaiting);
    }
    
    /// <summary>
    /// Признак что Процесс в обработке (есть активные очереди).
    /// </summary>
    /// <returns></returns>
    public virtual bool IsInProcess()
    {
      return GetProcessSettingQueue()
        .Where(_ => _.ProcessingStatus == JobManager.EntitiesQueueBatch.ProcessingStatus.InProcess)
        .Any();
    }
    
    /// <summary>
    /// Получить список очереди для процесса.
    /// </summary>
    /// <returns></returns>
    [Remote]
    public virtual IQueryable<IEntitiesQueueBatch> GetProcessSettingQueue()
    {
      return EntitiesQueueBatches.GetAll().Where(_ => _.ProcessId != null && _.ProcessId == _obj.ProcessId);
    }
    
    /// <summary>
    /// Создать запись очереди.
    /// </summary>
    /// <returns></returns>
    public virtual IEntitiesQueueBatch CreateEntitiesQueueBatch(List<long> range)
    {
      var queueItem = EntitiesQueueBatches.Create();
      queueItem.ProcessSetting = _obj;
      queueItem.ProcessId = _obj.ProcessId;
      queueItem.EntitiesIdRange = string.Join(Constants.Module.Delimeter.ToString(), range);
      queueItem.EntitiesCount = range.Count;
      queueItem.Save();
      
      return queueItem;
    }
    
    /// <summary>
    /// Получить количество потоков с учетом заданного лимита.
    /// </summary>
    [Public]
    public virtual int GetSettingFlowLimit(int availableFlowCount)
    {
      var settingFlowCount = GetFlowCount();
      return settingFlowCount > 0 && settingFlowCount < availableFlowCount
        ? settingFlowCount
        : availableFlowCount;
    }
    
    /// <summary>
    /// Получить максимальное количество потоков.
    /// </summary>
    [Public]
    public virtual int GetFlowCount()
    {
      var flowCount = _obj.FlowCountAtWorkingHours.HasValue && PublicFunctions.Module.IsWorkingTime()
        ? _obj.FlowCountAtWorkingHours.Value
        : _obj.FlowCount.GetValueOrDefault();
      
      if (flowCount == 0)
        flowCount = Functions.Module.GetTotalFlowCount();
      
      return flowCount;
    }
    
    /// <summary>
    /// Получить максимальное количество элементов в очереди.
    /// </summary>
    public virtual int GetBatchSize()
    {
      return _obj.BatchSize.GetValueOrDefault();
    }
    
    /// <summary>
    /// Получить количество сущностей для обработки.
    /// </summary>
    [Remote]
    public virtual long GetEntitiesCount()
    {
      return GetEntitiesIdsForProcessing().Count();
    }
    
    /// <summary>
    /// Обработать сущности.
    /// </summary>
    /// <param name="entitiesIds">Список ИД для обработки.</param>
    /// <returns>Структура с информацией о результатах обработки.</returns>
    public virtual Starkov.JobManager.Structures.Module.IProcessEntitiesResult ProcessEntities(List<long> entitiesIds)
    {
      var logger = Logger.WithLogger("ProcessEntities").WithProperty("settingId", _obj.Id).WithProperty("processId", _obj.ProcessId);
      logger.Debug("Start");
      
      var result = Starkov.JobManager.PublicFunctions.Module.CreateProcessEntitiesDefaultResult();
      var objectPerRequestCount = Constants.Module.GenericSettings.ObjectPerRequestCount;
      for (int skip = 0; skip < entitiesIds.Count; skip += objectPerRequestCount)
      {
        var batchIds = entitiesIds.Skip(skip).Take(objectPerRequestCount).ToList();
        var entities = GetEntitiesById(batchIds);
        foreach (var entity in entities)
        {
          try
          {
            if (_obj.IsLockDisable != true && !Locks.TryLock(entity))
            {
              result.UnsuccessItemsInfo.Add(Structures.Module.UnsuccessItem.Create(entity.Id, true, JobManager.ProcessSettingsBases.Resources.LockedByMessageFormat(Locks.GetLockInfo(entity).OwnerName), null));
              continue;
            }
            
            ProcessEntity(entity, logger);
          }
          catch(NotImplementedException ex)
          {
            logger.Error(ex, "Need to override method ProcessEntity(Sungero.Domain.Shared.IEntity entity, Sungero.Core.ILogger _logger)");
            result.UnsuccessItemsInfo.Add(Structures.Module.UnsuccessItem.Create(entity.Id, false, "При разработке не переопределен обязательный метод ProcessEntity", null));
          }
          catch (Exception ex)
          {
            logger.Error(ex, "Process failed for entity={0}", entity.Id);
            result.UnsuccessItemsInfo.Add(Structures.Module.UnsuccessItem.Create(entity.Id, false, ex.Message, ex.StackTrace));
          }
          finally
          {
            if (Locks.GetLockInfo(entity).IsLockedByMe)
              Locks.Unlock(entity);
          }
        }
      }
      
      logger.Debug("Done");
      
      if (result.UnsuccessItemsInfo.Any())
        result.IsSuccess = false;
      
      return result;
    }
    
    /// <summary>
    /// Обработать результат выполнения очереди.
    /// </summary>
    /// <param name="processResult">Структура с информацией об обработке.</param>
    /// <param name="setting">Настройка.</param>
    /// <param name="queue">Элемент очереди.</param>
    public virtual void QueueResultProcessing(Structures.Module.IProcessEntitiesResult processResult, IEntitiesQueueBatch queue)
    {
      if (processResult.ProcessStartTime.HasValue)
      {
        TimeSpan span = Calendar.Now - processResult.ProcessStartTime.Value;
        queue.ProcessingTime = (queue.ProcessingTime ?? 0) + span.TotalMinutes;
      }
      
      Functions.EntitiesQueueBatch.UpdateErrorsInfo(queue, processResult.UnsuccessItemsInfo);
      
      if (processResult.IsSuccess.GetValueOrDefault(true) == true)
        queue.ProcessingStatus = JobManager.EntitiesQueueBatch.ProcessingStatus.Complete;
      else if (queue.Iteration < _obj.RetryCount || processResult.UnsuccessItemsInfo.Any(_ => _.IsLocked == true))
        queue.ProcessingStatus = JobManager.EntitiesQueueBatch.ProcessingStatus.RetryWaiting;
      else
        queue.ProcessingStatus = JobManager.EntitiesQueueBatch.ProcessingStatus.Error;
    }
    
    /// <summary>
    /// Получить выборку сущностей по ИД.
    /// </summary>
    /// <param name="entitiesIds">Список ИД.</param>
    /// <returns>Элементы IEntity</returns>
    /// <remarks>Используется для действия "Показать списком" в справочнике "Очередь обработки".</remarks>
    [Public, Remote(IsPure = true)]
    public virtual List<Sungero.Domain.Shared.IEntity> GetEntitiesById(List<long> entitiesIds)
    {
      if (entitiesIds == null || !entitiesIds.Any())
        return new List<Sungero.Domain.Shared.IEntity>();
      
      return GetAllEntities().Where(_ => entitiesIds.Contains(_.Id)).ToList();
    }
    
    /// <summary>
    /// Получить идентификаторы сущностей для обработки, учитывая исключения и ошибки.
    /// </summary>
    public virtual IQueryable<long> GetEntitiesIdsConsideringExceptions()
    {
      var query = GetEntitiesIdsForProcessing();
      
      if (_obj.IsExcludeProcessedEntities == true)
      {
        var lastQueueBatch = GetProcessSettingQueue().OrderByDescending(_ => _.Id).FirstOrDefault();
        if (lastQueueBatch != null)
        {
          long lastId = PublicFunctions.EntitiesQueueBatch.GetLastEntityId(lastQueueBatch);
          query = query.Where(id => id > lastId);
        }
      }
      else
      {
        // Исключаем из обработки записи с ошибками
        var exceptedQueues = GetQueueErrors();
        query = query.Where(id => !exceptedQueues.Any(_ => _.Errors.Any(e => e.EntityId == id)));
      }
      
      return query.OrderBy(id => id);
    }
    
    #region Альтернативные методы для перекрытия в наследниках
    
    /// <summary>
    /// Получить идентификаторы сущностей для обработки.
    /// </summary>
    /// <exception cref="NotImplementedException">Генерируется, если метод GetAllEntities() не переопределен в наследнике.</exception>
    /// <remarks>Перекрыть если список ИД берется не из метода GetEntitiesForProcessing.</remarks>
    public virtual IQueryable<long> GetEntitiesIdsForProcessing()
    {
      return GetEntitiesForProcessing().Select(_ => _.Id);
    }
    
    #endregion

    #region Методы для перекрытия в наследниках
    
    /// <summary>
    /// Получить все сущности.
    /// </summary>
    /// <returns>Запрос сущностей без фильтрации.</returns>
    /// <remarks>
    /// Логика задается в наследниках.
    /// Метод необходим для оптимзации и гарантии получения сущностей при обработке.
    /// </remarks>
    public virtual IQueryable<Sungero.Domain.Shared.IEntity> GetAllEntities()
    {
      throw new NotImplementedException();
    }
    
    /// <summary>
    /// Фильтрация сущностей для обработки.
    /// </summary>
    /// <exception cref="NotImplementedException">Генерируется, если метод GetAllEntities() не переопределен в наследнике.</exception>
    /// <returns>Базовый метод возвращает все сущности.</returns>
    /// <remarks>
    /// Логика фильтрации задается в наследниках.
    /// Получение сущностей, которые требуется обработать.
    /// </remarks>
    public virtual IQueryable<Sungero.Domain.Shared.IEntity> GetEntitiesForProcessing()
    {
      return GetAllEntities();
    }

    /// <summary>
    /// Обработать сущность.
    /// </summary>
    /// <param name="entity">Сущность для обработки.</param>
    /// <param name="_logger">Преднастроенный экземпляр логгера с контекстом (settingId, processId).</param>
    /// <exception cref="NotImplementedException">Генерируется, если метод не переопределен в наследнике.</exception>
    /// <remarks>
    /// Логика задается в наследниках.
    /// Все исключения обрабатываются в вызывающем коде (ProcessEntities).
    /// </remarks>
    public virtual void ProcessEntity(Sungero.Domain.Shared.IEntity entity, Sungero.Core.ILogger _logger)
    {
      throw new NotImplementedException();
    }
    
    #endregion
    
  }
}