using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Starkov.JobManager.ProcessSettingsBase;

namespace Starkov.JobManager.Shared
{
  partial class ProcessSettingsBaseFunctions
  {

    /// <summary>
    /// Установить доступность свойств.
    /// </summary>       
    public virtual void SetEnabledProperties()
    {
      var properties = _obj.State.Properties;
      var isInProcess = _obj.ProcessStatus == ProcessStatus.InProcess || _obj.ProcessStatus == ProcessStatus.Suspended;
      
      var alwaysEnabled = new List<Sungero.Domain.Shared.IPropertyStateBase>()
      {
        properties.BatchSize,
        properties.Description,
        properties.Name,
        properties.RetryCount,
        properties.RetryInterval,
        properties.IsLockDisable,
        properties.FlowCount,
        properties.FlowCountAtWorkingHours,
        properties.Priority
      };
      
      foreach (var property in properties.Except(alwaysEnabled))
      {
        property.IsEnabled = !isInProcess;
      }
    }
    
    /// <summary>
    /// Записать в историю выполнение операции пользователем.
    /// </summary>
    /// <param name="operation">Enumeration операции (Должен быть ресурс формата Enum_Operation_)</param>
    public virtual void WriteActionToHistory(Sungero.Core.Enumeration operation)
    {
      var comment = JobManager.ProcessSettingsBases.Resources.HistoryComment_UserFormat(Users.Current.Name);
      _obj.History.Write(operation, null, comment);
    }
    
  }
}