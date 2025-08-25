using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Starkov.JobManager.EntitiesQueueBatch;

namespace Starkov.JobManager.Shared
{
  partial class EntitiesQueueBatchFunctions
  {

    /// <summary>
    /// Получить список ИД объектов.
    /// </summary>
    [Public]
    public virtual List<long> GetEntityIds()
    {
      if (_obj.Errors.Any())
        return _obj.Errors.Where(_ => _.EntityId.HasValue).Select(_ => _.EntityId.Value).ToList();
      
      var entityIds = new List<long>();
      
      long id;
      foreach (var textId in _obj.EntitiesIdRange.Split(Constants.Module.Delimeter))
      {
        if (long.TryParse(textId, out id))
          entityIds.Add(id);
        else
        {
          Logger.ErrorFormat("GetEntityIds From EntitiesQueueBatch={0}. Failed parse to long value «{1}»", _obj.Id, textId);
          var message = string.Format("Не удалось преобразовать в идентификатор значение {0}", textId);
          throw new ArgumentException(message);
        }
      }
      
      return entityIds;
    }
    
    /// <summary>
    /// Получить последний ИД из списка объектов.
    /// </summary>
    [Public]
    public virtual long GetLastEntityId()
    {
      long id = 0;
      var lastId = _obj.EntitiesIdRange.Split(Constants.Module.Delimeter).LastOrDefault();
      long.TryParse(lastId, out id);

      return id;
    }
    
    /// <summary>
    /// Обновить данные в коллекции ошибок.
    /// </summary>
    /// <param name="unsuccessItems">Коллекция структур с данными о неудачно обработанных сущностях.</param>
    public virtual void UpdateErrorsInfo(List<Structures.Module.IUnsuccessItem> unsuccessItems)
    {
      _obj.Errors.Clear();
      if (unsuccessItems.Count > 0)
        _obj.ErrorsCount = unsuccessItems.Count;
      else
        _obj.ErrorsCount = null;
      
      foreach (var unsuccessItem in unsuccessItems)
      {
        var row = _obj.Errors.AddNew();
        row.EntityId = unsuccessItem.EntityId;
        
        if (!string.IsNullOrEmpty(unsuccessItem.ErrorMessage))
          row.ErrorMessage = unsuccessItem.ErrorMessage.Length > row.Info.Properties.ErrorMessage.Length
            ? unsuccessItem.ErrorMessage.Substring(0, row.Info.Properties.ErrorMessage.Length)
            : unsuccessItem.ErrorMessage;
        
        if (!string.IsNullOrEmpty(unsuccessItem.StackTrace))
          row.StackTrace = unsuccessItem.StackTrace;
      }
    }

  }
}