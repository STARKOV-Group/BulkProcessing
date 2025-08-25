using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Starkov.JobManager.Structures.Module
{

  /// <summary>
  /// Результат обработки сущностей.
  /// </summary>
  [Public]
  partial class ProcessEntitiesResult
  {
        
    /// <summary>
    /// Время старта обработки.
    /// </summary>
    public DateTime? ProcessStartTime { get; set; }
    
    /// <summary>
    /// Признак успешной обработки.
    /// </summary>
    public bool? IsSuccess { get; set; }
    
    /// <summary>
    /// Список сущностей, которые не удалось обработать.
    /// </summary>
    public List<Starkov.JobManager.Structures.Module.IUnsuccessItem> UnsuccessItemsInfo { get; set; }
    
    /// <summary>
    /// Расширенные параметры для использования в перекрытиях.
    /// </summary>
    public Dictionary<string, string> ExtendedParams { get; set; }
  }
  
  [Public]
  partial class UnsuccessItem
  {
    /// <summary>
    /// ИД сущности, которую не удалось обработать.
    /// </summary>
    public long EntityId { get; set; }
    
    /// <summary>
    /// Признак что сущность заблокирована.
    /// </summary>
    public bool IsLocked { get; set; }
    
    /// <summary>
    /// Сообщение об ошибке.
    /// </summary>
    public string ErrorMessage { get; set; }
    
    /// <summary>
    /// Стэк ошибки.
    /// </summary>
    public string StackTrace { get; set; }
  }

}