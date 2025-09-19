using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Starkov.JobManager.Server
{
  public class ModuleFunctions
  {

    /// <summary>
    /// Обновить общее значение максимального количества потоков.
    /// </summary>
    public virtual void UpdateTotalFlowCount(int totalFlowCount)
    {
      Sungero.Docflow.PublicFunctions.Module.InsertOrUpdateDocflowParam(Constants.Module.GenericSettings.TotalFlowCountParamName, totalFlowCount.ToString());
    }
    
    /// <summary>
    /// Получить общее значение максимального количества потоков.
    /// </summary>
    public virtual int GetTotalFlowCount()
    {
      int flowCount = 0;
      var param = Sungero.Docflow.PublicFunctions.Module.GetDocflowParamsValue(Constants.Module.GenericSettings.TotalFlowCountParamName);
      if (param != null)
        int.TryParse(param.ToString(), out flowCount);
      
      return flowCount;
    }
    
    /// <summary>
    /// Получить активные элементы очереди.
    /// </summary>
    public virtual IQueryable<IEntitiesQueueBatch> GetQueueBatchesInProcess()
    {
      var queueBatches = EntitiesQueueBatches.GetAll()
        .Where(q => q.ProcessSetting != null && q.ProcessSetting.Status == JobManager.ProcessSettingsBase.Status.Active)
        .Where(q => q.ProcessingStatus == JobManager.EntitiesQueueBatch.ProcessingStatus.InProcess)
        .Where(q => q.EntitiesIdRange != null && q.EntitiesIdRange != string.Empty);
      
      return queueBatches;
    }
    
    /// <summary>
    /// Получить заготовку стркутуры для результата обработки.
    /// </summary>
    /// <returns>Заготовка структуры.</returns>
    [Public]
    public static Starkov.JobManager.Structures.Module.IProcessEntitiesResult CreateProcessEntitiesDefaultResult()
    {
      var defaultResult = Structures.Module.ProcessEntitiesResult.Create();
      defaultResult.UnsuccessItemsInfo = new List<Structures.Module.IUnsuccessItem>();
      defaultResult.IsSuccess = true;
      return defaultResult;
    }
    
    /// <summary>
    /// Проверка что текущее время является рабочим.
    /// </summary>
    /// <returns>true если текущее время является рабочим.</returns>
    [Public]
    public virtual bool IsWorkingTime()
    {
      return Calendar.IsWorkingTime(Calendar.Now);
    }

  }
}