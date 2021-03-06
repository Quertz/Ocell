﻿using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using DanielVaughan.ComponentModel;
using DanielVaughan;
using DanielVaughan.Windows;
using Ocell.Library.Twitter;
using Ocell.Library;
using System.Linq;

namespace Ocell.Pages.Search
{
    public class ResourceViewModel : ExtendedViewModelBase
    {
        public static TwitterResource Resource;

        string pageTitle;
        public string PageTitle
        {
            get { return pageTitle; }
            set { Assign("PageTitle", ref pageTitle, value); }
        }

        TweetLoader loader;
        public TweetLoader Loader
        {
            get { return loader; }
            set { Assign("Loader", ref loader, value); }
        }

        DelegateCommand addCommand;
        public ICommand AddCommand
        {
            get { return addCommand; }
        }

        public ResourceViewModel()
            : base("Search")
        {
            PageTitle = Resource.Title;

            this.PropertyChanged += (sender, property) =>
                {
                    if (property.PropertyName == "Loader")
                        UpdateTweetLoader();
                };

            addCommand = new DelegateCommand((param) =>
                {
                    if (!Config.Columns.Contains(Loader.Resource))
                        Config.Columns.Add(Loader.Resource);
                    Config.SaveColumns();
                    MessageService.ShowMessage(Localization.Resources.ColumnAdded, "");
                    DataTransfer.ShouldReloadColumns = true;
                    addCommand.RaiseCanExecuteChanged();
                },
                (param) => !Config.Columns.Contains(Resource));
        }

        public void UpdateTweetLoader()
        {
            Loader.Resource = Resource;
            Loader.Load();
        }
    }
}
