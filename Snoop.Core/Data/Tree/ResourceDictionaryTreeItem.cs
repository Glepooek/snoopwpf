﻿// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop.Data.Tree
{
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Data;

    public class ResourceDictionaryTreeItem : TreeItem
    {
        private static readonly SortDescription dictionarySortDescription = new(nameof(SortOrder), ListSortDirection.Ascending);
        private static readonly SortDescription displayNameSortDescription = new(nameof(DisplayName), ListSortDirection.Ascending);

        private readonly ResourceDictionary dictionary;
        private TreeItem? placeholderChild;

        public ResourceDictionaryTreeItem(ResourceDictionary dictionary, TreeItem? parent, TreeService treeService)
            : base(dictionary, parent, treeService)
        {
            this.dictionary = dictionary;

            var childrenView = CollectionViewSource.GetDefaultView(this.Children);
            childrenView.SortDescriptions.Add(dictionarySortDescription);
            childrenView.SortDescriptions.Add(displayNameSortDescription);
        }

        public override TreeItem? FindNode(object? target)
        {
            return null;
        }

        protected override string GetName()
        {
            var source = this.dictionary.Source?.ToString();

            if (string.IsNullOrEmpty(source))
            {
                return base.GetName();
            }

            return source!;
        }

        protected override void ReloadCore()
        {
            if (this.IsExpanded)
            {
                this.ReallyLoadChildren();
                return;
            }

            if (this.dictionary.MergedDictionaries.Count > 0
                || this.dictionary.Keys.Count > 0)
            {
                this.placeholderChild = new ResourceItem("Placeholder", "Placeholder", this, this.TreeService, false);
                this.AddChild(this.placeholderChild);
            }
        }

        private void ReallyLoadChildren()
        {
            if (this.placeholderChild is not null)
            {
                if (this.RemoveChild(this.placeholderChild) == false)
                {
                    return;
                }
            }

            var order = 0;
            var resourceDictionary = this.dictionary;

            foreach (var mergedDictionary in resourceDictionary.MergedDictionaries)
            {
                var resourceDictionaryItem = new ResourceDictionaryTreeItem(mergedDictionary, this, this.TreeService)
                {
                    SortOrder = order
                };
                resourceDictionaryItem.Reload();

                this.AddChild(resourceDictionaryItem);

                ++order;
            }

            foreach (var key in resourceDictionary.Keys)
            {
                resourceDictionary.TryGetValue(key, out var item, out var exception);

                if (item is not null
                    || exception is not null
                    || key is not null)
                {
                    this.AddChild(new ResourceItem(item ?? exception ?? key!, key, this, this.TreeService, exception is not null));
                }
            }
        }

        protected override void OnPropertyChanged(string propertyName)
        {
            base.OnPropertyChanged(propertyName);

            if (propertyName is nameof(this.IsExpanded)
                && this.IsExpanded)
            {
                this.ReallyLoadChildren();
            }
        }

        public override string ToString()
        {
            var source = this.dictionary.Source?.ToString() ?? "Runtime Dictionary";

            var childrenCount = this.dictionary.MergedDictionaries.Count + this.dictionary.Keys.Count;

            return $"{childrenCount} resources ({source})";
        }
    }

    public class ResourceItem : TreeItem
    {
        private readonly object? key;
        private readonly bool hasError;

        public ResourceItem(object target, object? key, TreeItem parent, TreeService treeService, bool hasError)
            : base(target, parent, treeService)
        {
            this.key = key;
            this.hasError = hasError;
            this.SortOrder = int.MaxValue;

            this.ShouldBeAnalyzed = false;
        }

        public override string DisplayName => this.key?.ToString() ?? "{x:Null}";

        public override string ToString()
        {
            if (this.hasError)
            {
                return $"{this.DisplayName} (Invalid resource definition)";
            }

            if (ReferenceEquals(this.key, this.Target))
            {
                return this.DisplayName;
            }

            return $"{this.DisplayName} ({this.Target.GetType().Name})";
        }
    }
}