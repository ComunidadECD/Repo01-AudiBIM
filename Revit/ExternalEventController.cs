using System;
using System.Collections.Generic;
using Autodesk.Revit.UI;

namespace BIMQualityAuditor.Revit
{
    public class ExternalEventController : IExternalEventHandler
    {
        private readonly Queue<Action<UIApplication>> _tasks = new();
        private readonly ExternalEvent _externalEvent;

        public ExternalEventController()
        {
            _externalEvent = ExternalEvent.Create(this);
        }

        public void EnqueueTask(Action<UIApplication> task)
        {
            lock (_tasks)
            {
                _tasks.Enqueue(task);
            }
            _externalEvent.Raise();
        }

        public void Execute(UIApplication app)
        {
            Action<UIApplication>? task = null;
            lock (_tasks)
            {
                if (_tasks.Count > 0)
                {
                    task = _tasks.Dequeue();
                }
            }

            try
            {
                task?.Invoke(app);
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error de Ejecución Revit", ex.Message);
            }
        }

        public string GetName() => "BIMQualityAuditor_ExternalEventHandler";
    }
}
