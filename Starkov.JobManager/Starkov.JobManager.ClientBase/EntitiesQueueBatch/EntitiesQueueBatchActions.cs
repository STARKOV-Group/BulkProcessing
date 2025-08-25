using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Starkov.JobManager.EntitiesQueueBatch;

namespace Starkov.JobManager.Client
{
  partial class EntitiesQueueBatchErrorsActions
  {

    public virtual bool CanShowEntity(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      return true;
    }

    public virtual void ShowEntity(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      var processSetting = EntitiesQueueBatches.As(e.RootEntity).ProcessSetting;
      var entityId = new List<long>() { _obj.EntityId.GetValueOrDefault() };
      var entity = PublicFunctions.ProcessSettingsBase.Remote.GetEntitiesById(processSetting, entityId).FirstOrDefault();
      if (entity != null)
        entity.ShowModal();
    }
  }


  partial class EntitiesQueueBatchCollectionActions
  {

    public virtual bool CanScheduled(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _objs.All(_ => _.ProcessingStatus == ProcessingStatus.Error);
    }

    public virtual void Scheduled(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      foreach (var queue in _objs)
      {
        queue.ProcessingStatus = ProcessingStatus.Scheduled;
        queue.Save();
      }
      
      JobManager.Jobs.CreateEntitiesQueueBatches.Enqueue();
    }
  }

  partial class EntitiesQueueBatchActions
  {
    public virtual void ShowEntity1(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      
    }

    public virtual bool CanShowEntity1(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }



    public override void CopyEntity(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.CopyEntity(e);
    }

    public override bool CanCopyEntity(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return false;
    }

    public virtual bool CanShowEntitiesRange(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

    public virtual void ShowEntitiesRange(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var range = PublicFunctions.ProcessSettingsBase.Remote.GetEntitiesById(_obj.ProcessSetting, Functions.EntitiesQueueBatch.GetEntityIds(_obj));
      range.ShowModal("Объекты очереди");
    }

  }

}