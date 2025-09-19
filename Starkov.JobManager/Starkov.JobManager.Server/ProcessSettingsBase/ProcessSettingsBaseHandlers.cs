using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Starkov.JobManager.ProcessSettingsBase;

namespace Starkov.JobManager
{
  partial class ProcessSettingsBaseCreatingFromServerHandler
  {

    public override void CreatingFrom(Sungero.Domain.CreatingFromEventArgs e)
    {
      e.Without(_info.Properties.ProcessId);
    }
  }

  partial class ProcessSettingsBaseServerHandlers
  {

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      if (_obj.BatchSize > Constants.Module.GenericSettings.DefaultMaxBatchSize)
      {
        e.AddError(_obj.Info.Properties.BatchSize, JobManager.ProcessSettingsBases.Resources.BatchLimitErrorMessageFormat(Constants.Module.GenericSettings.DefaultMaxBatchSize));
        return;
      }
      
      var totalFlowCount = Functions.Module.GetTotalFlowCount();
      if (totalFlowCount < _obj.FlowCount)
        e.AddError(_obj.Info.Properties.FlowCount, Starkov.JobManager.ProcessSettingsBases.Resources.FlowCountExceedErrorFormat(totalFlowCount));
    }

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      if (!_obj.State.IsCopied)
      {
        _obj.Name = _obj.Info.LocalizedName;
        _obj.Priority = 0;
        _obj.FlowCount = 1;
        _obj.BatchSize = Constants.Module.GenericSettings.ObjectPerRequestCount;
        _obj.RetryCount = 0;
        _obj.RetryInterval = 10;
        _obj.IsExcludeProcessedEntities = true;
        _obj.IsLockDisable = false;
      }
      
      _obj.ProcessStatus = ProcessStatus.Draft;
    }
  }

}