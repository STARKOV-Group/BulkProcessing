using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Starkov.JobManager.EntitiesQueueBatch;

namespace Starkov.JobManager
{
  partial class EntitiesQueueBatchServerHandlers
  {

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      if (_obj.State.IsChanged)
        _obj.Modified = Calendar.Now;
    }

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      _obj.Iteration = 0;
      _obj.ProcessingStatus = ProcessingStatus.Scheduled;
      _obj.Created = Calendar.Now;
    }
  }

}