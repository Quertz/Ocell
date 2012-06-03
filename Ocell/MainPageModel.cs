﻿using System.Collections.ObjectModel;
using System.Windows.Data;
using System.Windows.Input;
using DanielVaughan.ComponentModel;
using DanielVaughan.Windows;
using TweetSharp;
using Ocell.Library;
using Ocell.Library.Twitter;
using System.Collections.Generic;
using System;
using System.ComponentModel;
using System.Threading;
using DanielVaughan;
using DanielVaughan.InversionOfControl;
using DanielVaughan.Net;
using DanielVaughan.Services;
using System.Linq;
using Ocell.Library.Filtering;

namespace Ocell
{
    public class BroadcastArgs : EventArgs
    {
        public TwitterResource Resource { get; set; }
        public bool BroadcastAll { get; set; }

        public BroadcastArgs(TwitterResource resource, bool broadcastToAll = false)
        {
            Resource = resource;
            BroadcastAll = broadcastToAll;
        }
    }

    public delegate void BroadcastEventHandler(object sender, BroadcastArgs e);

    public class MainPageModel : ViewModelBase
    {
        DateTime lastAutoReload;
        const int secondsBetweenReloads = 25;

        public event BroadcastEventHandler ScrollToTop;
        public void RaiseScrollToTop(TwitterResource resource)
        {
            var temp = ScrollToTop;
            if (temp != null)
                temp(this, new BroadcastArgs(resource));
        }

        public event BroadcastEventHandler ReloadLists;
        private void RaiseReload(TwitterResource resource)
        {
            var temp = ReloadLists;
            if (temp != null)
                temp(this, new BroadcastArgs(resource, false));
        }

        private void RaiseReloadAll()
        {
            var temp = ReloadLists;
            if (temp != null)
                temp(this, new BroadcastArgs(Config.Columns.FirstOrDefault(), true));
        }

        bool isLoading;
        public bool IsLoading
        {
            get { return isLoading; }
            set { Assign("IsLoading", ref isLoading, value); }
        }

        int loadingCount;
        public int LoadingCount
        {
            get { return loadingCount; }
            set
            {
                loadingCount = value;
                if (loadingCount <= 0)
                    IsLoading = false;
                else
                    IsLoading = true;
            }

        }

        SafeObservable<TwitterResource> pivots;
        public SafeObservable<TwitterResource> Pivots
        {
            get { return pivots; }
            set { Assign("Pivots", ref pivots, value); }
        }

        object selectedPivot;
        public object SelectedPivot
        {
            get { return selectedPivot; }
            set { Assign("SelectedPivot", ref selectedPivot, value); }
        }

        string currentAccountName;
        public string CurrentAccountName
        {
            get { return currentAccountName; }
            set { Assign("CurrentAccountName", ref currentAccountName, value); }
        }

        bool isSearching;
        public bool IsSearching
        {
            get { return isSearching; }
            set { Assign("IsSearching", ref isSearching, value); }
        }

        string userSearch;
        public string UserSearch
        {
            get { return userSearch; }
            set { Assign("UserSearch", ref userSearch, value); }
        }

        #region Commands
        DelegateCommand pinToStart;
        public ICommand PinToStart
        {
            get { return pinToStart; }
        }

        DelegateCommand filterColumn;
        public ICommand FilterColumn
        {
            get { return filterColumn; }
        }

        DelegateCommand toMyProfile;
        public ICommand ToMyProfile
        {
            get { return toMyProfile; }
        }

        DelegateCommand goToUser;
        public ICommand GoToUser
        {
            get { return goToUser; }
        }
        #endregion

        private void SetUpCommands()
        {
            pinToStart = new DelegateCommand((obj) =>
                {
                    var column = (TwitterResource)SelectedPivot;
                    if (SecondaryTiles.ColumnTileIsCreated(column))
                        MessageService.ShowError("This column is already pinned.");
                    else
                    {
                        SecondaryTiles.CreateColumnTile(column);
                        MessageService.ShowLightNotification("Pinned!");
                    }
                }, (obj) => SelectedPivot != null);

            filterColumn = new DelegateCommand((obj) =>
            {
                var column = (TwitterResource)SelectedPivot;
                DataTransfer.cFilter = Config.Filters.FirstOrDefault(item => item.Resource == column);

                if (DataTransfer.cFilter == null)
                    DataTransfer.cFilter = new ColumnFilter { Resource = column };

                DataTransfer.IsGlobalFilter = false;
                Navigate(Uris.Filters);

            }, (obj) => SelectedPivot != null);

            toMyProfile = new DelegateCommand((obj) =>
                {
                    Navigate("/Pages/Elements/User.xaml?user=" + CurrentAccountName);
                }, (obj) => CurrentAccountName != null);

            goToUser = new DelegateCommand((obj) =>
            {
                IsSearching = false;
                Navigate("/Pages/Elements/User.xaml?user=" + UserSearch);
            });

        }

        public MainPageModel()
            : base("MainPage")
        {
            if (Config.RetweetAsMentions == null)
                Config.RetweetAsMentions = true;
            if (Config.TweetsPerRequest == null)
                Config.TweetsPerRequest = 40;
            if (Config.DefaultMuteTime == null || Config.DefaultMuteTime == TimeSpan.FromHours(0))
                Config.DefaultMuteTime = TimeSpan.FromHours(8);

            lastAutoReload = DateTime.MinValue;
            Pivots = new SafeObservable<TwitterResource>();

            foreach (var pivot in Config.Columns)
                Pivots.Add(pivot);

            Config.Columns.CollectionChanged += (sender, e) =>
            {
                foreach (var item in e.NewItems)
                {
                    if ((item is TwitterResource) && !Pivots.Contains((TwitterResource)item))
                        Pivots.Add((TwitterResource)item);
                }

                foreach (var item in e.OldItems)
                {
                    if ((item is TwitterResource) && Pivots.Contains((TwitterResource)item))
                        Pivots.Remove((TwitterResource)item);
                }
            };

            this.PropertyChanged += (sender, e) =>
                {
                    if (e.PropertyName == "SelectedPivot")
                        UpdatePivot();
                };

            string column;
            if (QueryParameters.TryGetValue("column", out column))
            {
                column = Uri.UnescapeDataString(column);
                if(Config.Columns.Any(item => item.String == column))
                    SelectedPivot = Config.Columns.First(item => item.String == column);
            }

            SetUpCommands();
        }

        void UpdatePivot()
        {
            if (SelectedPivot is TwitterResource)
            {
                var resource = (TwitterResource)SelectedPivot;
                CurrentAccountName = resource.User.ScreenName.ToUpperInvariant();
                if (DateTime.Now > lastAutoReload.AddSeconds(secondsBetweenReloads))
                {
                    lastAutoReload = DateTime.Now;
                    ThreadPool.QueueUserWorkItem((context) => RaiseReload(resource));
                }
                DataTransfer.CurrentAccount = resource.User;
            }
        }

        public void RaiseNavigatedTo(object sender, System.Windows.Navigation.NavigationEventArgs e) 
        {
            ThreadPool.QueueUserWorkItem((context) => RaiseReloadAll());
        }
    }
}
