using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using LM01_UI.ViewModels;

namespace LM01_UI
{
    public class ViewLocator : IDataTemplate
    {

        public Control? Build(object? param)
        {
            if (param is null)
                return null;

            // App.axaml already provides DataTemplates for most view models;
            // this locator is a fallback used when no explicit template exists.
            var vmName = param.GetType().FullName!;
            var viewName = vmName.Replace(".ViewModels.", ".Views.")
                                 .Replace("ViewModel", "View");
            var type = Type.GetType(viewName);

            if (type != null)
            {
                return (Control)Activator.CreateInstance(type)!;
            }

            return new TextBlock { Text = "Not Found: " + viewName };
        }

        public bool Match(object? data)
        {
            return data is ViewModelBase;
        }
    }
}
