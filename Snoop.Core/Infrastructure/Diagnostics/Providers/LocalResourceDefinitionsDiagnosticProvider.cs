﻿namespace Snoop.Infrastructure.Diagnostics.Providers
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Media;
    using Snoop.Data.Tree;

    public class LocalResourceDefinitionsDiagnosticProvider : DiagnosticProvider
    {
        public override string Name => "Local definition of resources";

        public override string Description => "Searches for locally defined resources.";

        protected override IEnumerable<DiagnosticItem> GetDiagnosticItemsInternal(TreeItem treeItem)
        {
            if (treeItem.Target is not DependencyObject dependencyObject)
            {
                yield break;
            }

            foreach (PropertyDescriptor? property in TypeDescriptor.GetProperties(dependencyObject.GetType()))
            {
                if (property is null)
                {
                    continue;
                }

                var dpd = DependencyPropertyDescriptor.FromProperty(property);

                if (dpd is null
                    || dpd.IsReadOnly)
                {
                    continue;
                }

                if (typeof(Color).IsAssignableFrom(dpd.PropertyType)
                    || typeof(Brush).IsAssignableFrom(dpd.PropertyType))
                {
                    var localValue = dependencyObject.ReadLocalValue(dpd.DependencyProperty);
                    var valueSource = DependencyPropertyHelper.GetValueSource(dependencyObject, dpd.DependencyProperty);

                    if (valueSource.BaseValueSource == BaseValueSource.Local
                        && valueSource.IsExpression == false)
                    {
                        switch (localValue)
                        {
                            case Brush brush when brush != Brushes.Transparent:
                                yield return
                                    new(this,
                                        "LocalBrush",
                                        $"Property '{dpd.DisplayName}' contains the local brush '{localValue}'. Prevent local brushes to keep the design maintainable.",
                                        DiagnosticArea.Maintainability,
                                        DiagnosticLevel.Info)
                                    {
                                        TreeItem = treeItem,
                                        Dispatcher = dependencyObject.Dispatcher,
                                        SourceObject = dependencyObject
                                    };
                                break;

                            case Color color when color != Colors.Transparent:
                                yield return
                                    new(this,
                                        "LocalColor",
                                        $"Property '{dpd.DisplayName}' contains the local color '{localValue}'. Prevent local colors to keep the design maintainable.",
                                        DiagnosticArea.Maintainability,
                                        DiagnosticLevel.Info)
                                    {
                                        TreeItem = treeItem,
                                        Dispatcher = dependencyObject.Dispatcher,
                                        SourceObject = dependencyObject
                                    };
                                break;
                        }
                    }
                }
            }
        }
    }
}