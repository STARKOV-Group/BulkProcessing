using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Starkov.JobManager.ProcessSettingsBase;

namespace Starkov.JobManager.Client
{
  partial class ProcessSettingsBaseActions
  {
    public virtual void Stop(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (!Dialogs.CreateConfirmDialog("Остановить обработку и очистить очередь?").Show())
        return;
      
      _obj.ProcessId = null;
      _obj.ProcessStatus = ProcessSettingsBase.ProcessStatus.Done;
      
      Functions.ProcessSettingsBase.WriteActionToHistory(_obj, new Enumeration("ProcessStop"));
      _obj.Save();
    }

    public virtual bool CanStop(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.ProcessStatus == ProcessSettingsBase.ProcessStatus.InProcess || _obj.ProcessStatus == ProcessSettingsBase.ProcessStatus.Suspended;
    }


    public virtual void Pause(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      Functions.ProcessSettingsBase.WriteActionToHistory(_obj, new Enumeration("ProcessPause"));
      _obj.ProcessStatus = ProcessSettingsBase.ProcessStatus.Suspended;
      _obj.Save();
    }

    public virtual bool CanPause(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.ProcessStatus == ProcessSettingsBase.ProcessStatus.InProcess;
    }

    public virtual void Start(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      _obj.ProcessStatus = ProcessSettingsBase.ProcessStatus.InProcess;
      
      if (string.IsNullOrEmpty(_obj.ProcessId))
        _obj.ProcessId = Guid.NewGuid().ToString();
      
      Functions.ProcessSettingsBase.WriteActionToHistory(_obj, new Enumeration("ProcessStart"));
      _obj.Save();
      
      Jobs.CreateEntitiesQueueBatches.Enqueue();
    }

    public virtual bool CanStart(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.Status == Status.Active && _obj.ProcessStatus != ProcessSettingsBase.ProcessStatus.InProcess;
    }

    public virtual void ShowCount(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var count = Functions.ProcessSettingsBase.Remote.GetEntitiesCount(_obj);
      e.AddInformation(string.Format("Согласно критериям текущая выборка к обработке составляет {0} записей.", count));
    }

    public virtual bool CanShowCount(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }


    public virtual void ShowQueue(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      Functions.ProcessSettingsBase.Remote.GetProcessSettingQueue(_obj).Show();
    }

    public virtual bool CanShowQueue(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

    public virtual void ShowErrorsList(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      Functions.ProcessSettingsBase.Remote.GetQueueErrors(_obj).Show();
    }

    public virtual bool CanShowErrorsList(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

  }

}