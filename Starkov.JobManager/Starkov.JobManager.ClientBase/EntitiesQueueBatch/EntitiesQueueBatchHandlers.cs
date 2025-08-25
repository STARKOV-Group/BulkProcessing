using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Starkov.JobManager.EntitiesQueueBatch;

namespace Starkov.JobManager
{
  partial class EntitiesQueueBatchClientHandlers
  {

    public override void Showing(Sungero.Presentation.FormShowingEventArgs e)
    {
      if (Functions.EntitiesQueueBatch.Remote.GetSetting(_obj) == null)
        e.HideAction(_obj.Info.Actions.Scheduled);
      
      if (!_obj.Errors.Any())
        _obj.State.Pages.Errors.IsVisible = false;
      else
        _obj.State.Pages.Errors.Activate();
    }

  }
}