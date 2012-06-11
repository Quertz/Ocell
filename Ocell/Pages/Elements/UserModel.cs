﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows.Input;
using DanielVaughan.ComponentModel;
using DanielVaughan.Windows;
using Microsoft.Phone.Tasks;
using Ocell.Library;
using Ocell.Library.Twitter;
using TweetSharp;

namespace Ocell.Pages.Elements
{
    public class UserModel : ViewModelBase
    {
        public TwitterUser User { get; set; } // Not a property, don't need Assign().

        bool isLoading;
        public bool IsLoading
        {
            get { return isLoading; }
            set { Assign("IsLoading", ref isLoading, value); }
        }

        string barText;
        public string BarText
        {
            get { return barText; }
            set { Assign("BarText", ref barText, value); }
        }

        bool followed;
        public bool Followed
        {
            get { return followed; }
            set { Assign("Followed", ref followed, value); }
        }

        bool blocked;
        public bool Blocked
        {
            get { return blocked; }
            set { Assign("Blocked", ref blocked, value); }
        }

        bool isOwner;
        public bool IsOwner
        {
            get { return isOwner; }
            set { Assign("IsOwner", ref isOwner, value); }
        }

        #region Fields.
        string avatar;
        public string Avatar
        {
            get { return avatar; }
            set { Assign("Avatar", ref avatar, value); }
        }

        string name;
        public string Name
        {
            get { return name; }
            set { Assign("Name", ref name, value); }
        }

        string screenName;
        public string ScreenName
        {
            get { return screenName; }
            set { Assign("ScreenName", ref screenName, value); }
        }

        string website;
        public string Website
        {
            get { return website; }
            set { Assign("Website", ref website, value); }
        }

        string biography;
        public string Biography
        {
            get { return biography; }
            set { Assign("Biography", ref biography, value); }
        }

        string tweets;
        public string Tweets
        {
            get { return tweets; }
            set { Assign("Tweets", ref tweets, value); }
        }

        string following;
        public string Following
        {
            get { return following; }
            set { Assign("Following", ref following, value); }
        }

        string followers;
        public string Followers
        {
            get { return followers; }
            set { Assign("Followers", ref followers, value); }
        }

        bool websiteEnabled;
        public bool WebsiteEnabled
        {
            get { return websiteEnabled; }
            set { Assign("WebsiteEnabled", ref websiteEnabled, value); }
        }
        #endregion

        #region Commands
        Func<object, bool> GenericCanExecute;

        DelegateCommand followUser;
        public ICommand FollowUser
        {
            get { return followUser; }
        }

        DelegateCommand unfollowUser;
        public ICommand UnfollowUser
        {
            get { return unfollowUser; }
        }

        DelegateCommand pinUser;
        public ICommand PinUser
        {
            get { return pinUser; }
        }

        DelegateCommand block;
        public ICommand Block
        {
            get { return block; }
        }

        DelegateCommand unblock;
        public ICommand Unblock
        {
            get { return unblock; }
        }

        DelegateCommand reportSpam;
        public ICommand ReportSpam
        {
            get { return reportSpam; }
        }

        DelegateCommand manageLists;
        public ICommand ManageLists
        {
            get { return manageLists; }
        }

        DelegateCommand changeAvatar;
        public ICommand ChangeAvatar
        {
            get { return changeAvatar; }
        }
        #endregion

        public UserModel()
            : base("User")
        {
            GenericCanExecute = (obj) => User != null && DataTransfer.CurrentAccount != null;

            followUser = new DelegateCommand((obj) =>
            {
                IsLoading = true;
                ServiceDispatcher.GetService(DataTransfer.CurrentAccount).FollowUser(User.Id, ReceiveFollow);
            }, GenericCanExecute);

            unfollowUser = new DelegateCommand((obj) =>
            {
                IsLoading = true;
                ServiceDispatcher.GetService(DataTransfer.CurrentAccount).UnfollowUser(User.Id, ReceiveFollow);
            }, GenericCanExecute);

            pinUser = new DelegateCommand((obj) =>
                {
                    Config.Columns.Add(new TwitterResource
                    {
                        Data = User.ScreenName,
                        Type = ResourceType.Tweets,
                        User = DataTransfer.CurrentAccount
                    });
                    Config.SaveColumns();
                    MessageService.ShowLightNotification("User succesfully pinned.");
                    pinUser.RaiseCanExecuteChanged();

                }, item => GenericCanExecute.Invoke(null) 
                    && !Config.Columns.Any(o => o.Type == ResourceType.Tweets && o.Data == User.ScreenName));

            block = new DelegateCommand((obj) =>
                {
                    IsLoading = true;
                    ServiceDispatcher.GetService(DataTransfer.CurrentAccount).BlockUser(User.Id, ReceiveBlock);
                }, GenericCanExecute);

            unblock = new DelegateCommand((obj) =>
                {
                    IsLoading = true;
                    ServiceDispatcher.GetService(DataTransfer.CurrentAccount).UnblockUser(User.Id, ReceiveBlock);
                }, GenericCanExecute);

            reportSpam = new DelegateCommand((obj) =>
            {
                IsLoading = true;
                ServiceDispatcher.GetService(DataTransfer.CurrentAccount).ReportSpam(User.Id, ReceiveReportSpam);
            }, GenericCanExecute);

            changeAvatar = new DelegateCommand((obj) =>
                {
                    PhotoChooserTask task = new PhotoChooserTask();
                    task.ShowCamera = true;
                    task.Completed += new EventHandler<PhotoResult>(task_Completed);
                    task.Show();
                }, (obj) => GenericCanExecute.Invoke(null) && IsOwner);

            manageLists = new DelegateCommand((obj) => Navigate("/Pages/Lists/ListManager.xaml?user=" + User.ScreenName),
                GenericCanExecute);
        }

        public void Loaded(string userName)
        {
            Regex remove = new Regex("@|:");
            userName = remove.Replace(userName, "");

            ScreenName = userName;

            BarText = "Retrieving user...";
            IsLoading = true;
            ServiceDispatcher.GetDefaultService().ListUserProfilesFor(new List<string> { userName }, ReceiveUsers);
        }

        void task_Completed(object sender, PhotoResult e)
        {
            UserToken usr;
            usr = Config.Accounts.FirstOrDefault(item => item != null && item.ScreenName == User.ScreenName);
            if (e.TaskResult == TaskResult.OK && User != null)
            {
                BarText = "Uploading picture...";
                IsLoading = true;
                ITwitterService srv = ServiceDispatcher.GetService(usr);
                srv.UpdateProfileImage(e.OriginalFileName, e.ChosenPhoto, ReceivePhotoUpload);
            }
        }

        private void ReceivePhotoUpload(TwitterUser user, TwitterResponse response)
        {
            BarText = "";
            IsLoading = false;
            if (response.StatusCode == HttpStatusCode.OK)
                MessageService.ShowLightNotification("Your profile image has been changed.");
            else
                MessageService.ShowError("An error happened while uploading your image. Verify the file is less than 700 kB of size.");
        }

        void ReceiveFollow(TwitterUser usr, TwitterResponse response)
        {
            string action = Followed ? "unfollow" : "follow";
            IsLoading = false;
            if (response.StatusCode == HttpStatusCode.OK)
            {
                MessageService.ShowLightNotification("Now you " + action + " " + usr.ScreenName);
                Followed = !Followed;
                followUser.RaiseCanExecuteChanged();
                unfollowUser.RaiseCanExecuteChanged(); ;
            }
            else
                MessageService.ShowError("We couldn't " + action + " " + User.ScreenName + ".");
        }

        void ReceiveBlock(TwitterUser usr, TwitterResponse response)
        {
            string action = Blocked ? "unblock" : "block";
            string pastAction = Blocked ? "unblocked" : "blocked";
            IsLoading = false;

            if (response.StatusCode == HttpStatusCode.OK)
            {
                MessageService.ShowLightNotification(usr.ScreenName + " is now " + pastAction + ".");
                Blocked = !Blocked;
                block.RaiseCanExecuteChanged();
                unblock.RaiseCanExecuteChanged();
            }
            else
                MessageService.ShowError("We couldn't " + action + " " + User.ScreenName + ".");
        }

        void ReceiveReportSpam(TwitterUser usr, TwitterResponse response)
        {
            IsLoading = false;
            if (response.StatusCode == HttpStatusCode.OK)
                MessageService.ShowLightNotification("You have successfully reported and blocked " + usr.ScreenName);
            else
                MessageService.ShowError("We couldn't report the user.");
        }

        void ReceiveUsers(IEnumerable<TwitterUser> users, TwitterResponse response)
        {
            BarText = "";
            IsLoading = false;
            if (response.StatusCode != HttpStatusCode.OK || !users.Any())
            {
                MessageService.ShowError("We couldn't find the user @" + ScreenName);
                return;
            }

            User = users.First();

            Avatar = User.ProfileImageUrl;
            Name = User.Name;
            ScreenName = User.ScreenName;
            Website = User.Url;
            Biography = User.Description;
            Tweets = User.StatusesCount.ToString();
            Followers = User.FollowersCount.ToString();
            Following = User.FriendsCount.ToString();
            WebsiteEnabled = Uri.IsWellFormedUriString(User.Url, UriKind.Absolute);
            IsOwner = Config.Accounts.Any(item => item.Id == User.Id);

            if (DataTransfer.CurrentAccount != null)
            {
                var service = ServiceDispatcher.GetService(DataTransfer.CurrentAccount);
                service.GetFriendshipInfo((int)DataTransfer.CurrentAccount.Id, User.Id, ReceiveFriendshipInfo);
                service.ListBlockedUserIds(ReceiveBlockedUsers);
            }

            followUser.RaiseCanExecuteChanged();
            unfollowUser.RaiseCanExecuteChanged();
            block.RaiseCanExecuteChanged();
            unblock.RaiseCanExecuteChanged();
            pinUser.RaiseCanExecuteChanged();
            reportSpam.RaiseCanExecuteChanged();
            manageLists.RaiseCanExecuteChanged();
            changeAvatar.RaiseCanExecuteChanged();
        }

        void ReceiveFriendshipInfo(TwitterFriendship friendship, TwitterResponse response)
        {
            if (response.StatusCode != HttpStatusCode.OK)
            {
                MessageService.ShowWarning("We couldn't get your relationship with this user.");
                return;
            }

            Followed = friendship.Relationship.Source.Following;

            followUser.RaiseCanExecuteChanged();
            unfollowUser.RaiseCanExecuteChanged();
        }

        void ReceiveBlockedUsers(IEnumerable<int> blockedIds, TwitterResponse response)
        {
            Blocked = blockedIds.Any() && blockedIds.Contains(User.Id);

            block.RaiseCanExecuteChanged();
            unblock.RaiseCanExecuteChanged();
        }
    }
}
