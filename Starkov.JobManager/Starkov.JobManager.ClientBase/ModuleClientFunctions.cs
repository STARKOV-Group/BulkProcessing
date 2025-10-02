using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Starkov.JobManager.Client
{
  public partial class ModuleFunctions
  {

    /// <summary>
    /// Отобразить реестр настроек процессов.
    /// </summary>
    [LocalizeFunction("ProcessSettingsShowFunctionName", "ProcessSettingsShowFunctionDiscription")]
    public virtual void ProcessSettingsShow()
    {
      ProcessSettingsBases.GetAll().Show();
    }

  }
}