using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Starkov.JobManager.ProcessSettingsBase;

namespace Starkov.JobManager
{
  partial class ProcessSettingsBaseSharedHandlers
  {

    public virtual void IsSortIdDescendingChanged(Sungero.Domain.Shared.BooleanPropertyChangedEventArgs e)
    {
      if (Equals(e.OldValue, e.NewValue))
        return;
      
      if (e.NewValue == true)
        _obj.IsExcludeProcessedEntities = false;
      else
        _obj.IsExcludeProcessedEntities = _obj.State.Properties.IsExcludeProcessedEntities.PreviousValue;
    }

  }
}