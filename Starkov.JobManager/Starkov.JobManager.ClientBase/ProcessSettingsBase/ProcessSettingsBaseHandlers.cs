using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Starkov.JobManager.ProcessSettingsBase;

namespace Starkov.JobManager
{
  partial class ProcessSettingsBaseClientHandlers
  {

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      Functions.ProcessSettingsBase.SetEnabledProperties(_obj);
    }

    public override void Showing(Sungero.Presentation.FormShowingEventArgs e)
    {
      var errors = Functions.ProcessSettingsBase.Remote.GetQueueErrors(_obj);
      if (errors.Any())
      {
        e.AddWarning(JobManager.ProcessSettingsBases.Resources.QueueErrorsWarningMessage, _obj.Info.Actions.ShowErrorsList);
      }
    }

  }
}