using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using FastaAnalyzer.ModeleWidoku;

namespace FastaAnalyzer;

public class ViewLocator : IDataTemplate
{
    public Control? Build(object? param)
    {
        if (param is null)
            return null;

        var name = param.GetType().FullName!
            .Replace("ModeleWidoku", "Widoki")
            .Replace("ModelWidoku", "");

        var type = Type.GetType(name);

        if (type != null)
        {
            return (Control)Activator.CreateInstance(type)!;
        }

        return new TextBlock
        {
            Text = "Nie znaleziono widoku: " + name
        };
    }

    public bool Match(object? data)
    {
        return data is ViewModelBase;
    }
}