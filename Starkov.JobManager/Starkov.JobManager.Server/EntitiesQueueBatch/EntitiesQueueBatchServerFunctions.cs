using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Starkov.JobManager.EntitiesQueueBatch;

namespace Starkov.JobManager.Server
{
  partial class EntitiesQueueBatchFunctions
  {

    /// <summary>
    /// StateView с представлением ошибок.
    /// </summary>
    [Remote]
    public StateView GetEntitiesQueueBatchState()
    {
      var stateView = StateView.Create();
      
      var entityInfo = Functions.ProcessSettingsBase.GetAllEntities(_obj.ProcessSetting).FirstOrDefault()?.Info;
      
      foreach (var error in _obj.Errors)
      {
        var entityId = error.EntityId.GetValueOrDefault();
        var link = Hyperlinks.Get(entityInfo, entityId);
        var block = stateView.AddBlock();
        
        block.AddHyperlink(entityId.ToString(), link);
        block.AddLabel(error.ErrorMessage);
        
        if (!string.IsNullOrEmpty(error.StackTrace))
        {
          var child = block.AddChildBlock();
          child.AddLabel(error.StackTrace, true);
        }
      }
      
      return stateView;
    }

    /// <summary>
    /// Получить настройку для очереди.
    /// </summary>
    [Remote(IsPure = true)]
    public virtual JobManager.IProcessSettingsBase GetSetting()
    {
      return JobManager.ProcessSettingsBases.GetAll(_ => _.ProcessId == _obj.ProcessId).FirstOrDefault();
    }

  }
}