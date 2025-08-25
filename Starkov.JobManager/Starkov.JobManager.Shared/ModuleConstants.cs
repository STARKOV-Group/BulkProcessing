using System;
using Sungero.Core;

namespace Starkov.JobManager.Constants
{
  public static class Module
  {

    /// <summary>
    /// Символ разделителя для хранения коллекции в строке.
    /// </summary>
    public const char Delimeter = ';';
    
    public static class GenericSettings
    {
      /// <summary>
      /// Имя параметра для хранения общего значения максимального количества потоков.
      /// </summary>
      public const string TotalFlowCountParamName = "TotalFlowCount";
      
      /// <summary>
      /// Значение по-умолчанию для инициализации параметра общего значения максимального количества потоков.
      /// </summary>
      public const int DefaultFlowCountParamValue = 8;
      
       /// <summary>
      /// Значение по-умолчанию для максимального размера пачки сущностей в потоке.
      /// </summary>
      public const int DefaultMaxBatchSize = 1000;
      
      public const int ObjectPerRequestCount = 100;
    }

  }
}