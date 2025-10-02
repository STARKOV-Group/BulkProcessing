using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Domain.Initialization;

namespace Starkov.JobManager.Server
{
  public partial class ModuleInitializer
  {

    public override void Initializing(Sungero.Domain.ModuleInitializingEventArgs e)
    {
      CreateDocflowParams();
    }
    
    /// <summary>
    /// Создать параметры в таблице DocflowParams.
    /// </summary>
    public virtual void CreateDocflowParams()
    {
      if (Sungero.Docflow.PublicFunctions.Module.GetDocflowParamsValue(Constants.Module.GenericSettings.TotalFlowCountParamName) == null)
        Functions.Module.UpdateTotalFlowCount(Constants.Module.GenericSettings.DefaultFlowCountParamValue);
    }
  }
}
