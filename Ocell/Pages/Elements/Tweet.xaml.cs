﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Documents;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Tasks;
using Microsoft.Phone.Shell;
using Ocell.Library;
using Ocell.Library.Twitter;
using TweetSharp;


namespace Ocell.Pages.Elements
{
    public partial class Tweet : PhoneApplicationPage
    {
        public TweetSharp.TwitterStatus status;
        private bool _favBtnState;
        private ApplicationBarIconButton _favButton;

        public Tweet()
        {
            InitializeComponent(); 
            ThemeFunctions.ChangeBackgroundIfLightTheme(LayoutRoot);
            CreateFavButton();

            this.Loaded += new RoutedEventHandler(Tweet_Loaded); 
        }

        void CreateFavButton()
        {
            _favButton = new ApplicationBarIconButton();
            _favButton.IconUri = new Uri("/Images/Icons_White/appbar.favs.addto.rest.png", UriKind.Relative);
            _favButton.Click += new EventHandler(favButton_Click);
            _favButton.Text = "add favorite";
            _favBtnState = true;
            ApplicationBar.Buttons.Add(_favButton);
        }

        void ToggleFavButton()
        {
            if (_favBtnState)
            {
                _favButton.Text = "remove favorite";
                _favButton.IconUri = new Uri("/Images/Icons_White/appbar.favs.remove.rest.png", UriKind.Relative);
                _favBtnState = false;
            }
            else
            {
                _favButton.Text = "add favorite";
                _favButton.IconUri = new Uri("/Images/Icons_White/appbar.favs.addto.rest.png", UriKind.Relative);
                _favBtnState = true;
            }
        }

        void Tweet_Loaded(object sender, RoutedEventArgs e)
        {
            
            if (DataTransfer.Status == null)
            {
                Dispatcher.BeginInvoke(() => MessageBox.Show("Error loading the tweet. Sorry :("));
                NavigationService.GoBack();
                return;
            }

            if(DataTransfer.Status.RetweetedStatus != null)
                status = DataTransfer.Status.RetweetedStatus;
            else
                status = DataTransfer.Status;

            if (status.IsFavorited)
                Dispatcher.BeginInvoke(ToggleFavButton);

            if (status.User == null || status.User.Name == null)
                FillUser();

            SetBindings();
            CreateText(status);
            AdjustMargins();
            SetVisibilityOfRepliesAndImages();
            SetUsername();

            ContentPanel.UpdateLayout();
        }

        private void FillUser()
        {
            ServiceDispatcher.GetDefaultService().GetUserProfileFor(status.Author.ScreenName, ReceiveUser);
        }

        private void ReceiveUser(TwitterUser user, TwitterResponse response)
        {
            if (response.StatusCode != HttpStatusCode.OK)
                Dispatcher.BeginInvoke(() => MessageBox.Show("Couldn't get the full user profile."));
            status.User = user;
            Dispatcher.BeginInvoke(() =>
            {
                SetBindings();
                ContentPanel.UpdateLayout();
            });
        }

        private void SetBindings()
        {
            if (status != null)
            {
                ContentPanel.DataContext = null;
                ContentPanel.DataContext = status;
                if (status.Entities != null)
                {
                    ImagesList.DataContext = status.Entities.Media;
                    ImagesList.ItemsSource = status.Entities.Media.Where(item => item.MediaType == TwitterMediaType.Photo);
                }
            }
        }

        private void SetUsername()
        {
            string RTByText = "";
            if (DataTransfer.Status.RetweetedStatus != null)
                RTByText = " (RT by @" + DataTransfer.Status.Author.ScreenName + ")";

            SName.Text = "@" + status.Author.ScreenName + RTByText;
        }

        private void SetVisibilityOfRepliesAndImages()
        {
            if (status.InReplyToStatusId != null)
                Replies.Visibility = Visibility.Visible;

            if (status.Entities.Media.Count != 0)
                ImagesText.Visibility = Visibility.Visible;
        }

        private void AdjustMargins()
        {
            RemoveHTML conv = new RemoveHTML();
            RelativeDateTimeConverter dc = new RelativeDateTimeConverter();

            ViaDate.Margin = new Thickness(ViaDate.Margin.Left, Text.ActualHeight + Text.Margin.Top + 10,
                ViaDate.Margin.Right, ViaDate.Margin.Bottom);
            ViaDate.Text = (string)dc.Convert(status.CreatedDate, null, null, null) + " via " +
                conv.Convert(status.Source, null, null, null);

            Replies.Margin = new Thickness(Replies.Margin.Left, ViaDate.Margin.Top + 30,
                Replies.Margin.Right, Replies.Margin.Bottom);
        }

        private void CreateText(ITweetable Status)
        {
            var paragraph = new Paragraph();
            var runs = new List<Inline>();

            Text.Blocks.Clear();

            string TweetText = Status.Text;
            string PreviousText;
            int i = 0;

            foreach (var Entity in status.Entities)
            {
                if (Entity.StartIndex > i)
                {
                    PreviousText = TweetText.Substring(i, Entity.StartIndex - i);
                    runs.Add(new Run { Text = HttpUtility.HtmlDecode(PreviousText) });
                }

                i = Entity.EndIndex;

                switch (Entity.EntityType)
                {
                    case TwitterEntityType.HashTag:
                        runs.Add(CreateHashtagLink((TwitterHashTag)Entity));
                        break;

                    case TwitterEntityType.Mention:
                        runs.Add(CreateMentionLink((TwitterMention)Entity));
                        break;

                    case TwitterEntityType.Url:
                        runs.Add(CreateUrlLink((TwitterUrl)Entity));
                        break;
                    case TwitterEntityType.Media:
                        runs.Add(CreateMediaLink((TwitterMedia)Entity));
                        break;
                }
            }

            if (i < TweetText.Length)
                runs.Add(new Run{
                    Text = HttpUtility.HtmlDecode(TweetText.Substring(i))
                });

            foreach (var run in runs)
                paragraph.Inlines.Add(run);

            Text.Blocks.Add(paragraph);

            Text.UpdateLayout();
        }

        Inline CreateHashtagLink(TwitterHashTag Hashtag)
        {
            var link = new Hyperlink();
            link.Inlines.Add(new Run() { Text = "#" + Hashtag.Text });
            link.FontWeight = FontWeights.Bold;
            link.TextDecorations = null;
            link.TargetName = "#" + Hashtag.Text;
            link.Click += new RoutedEventHandler(link_Click);

            return link;
        }

        Inline CreateMentionLink(TwitterMention Mention)
        {
            var link = new Hyperlink();
            link.Inlines.Add(new Run() { Text = "@" + Mention.ScreenName });
            link.FontWeight = FontWeights.Bold;
            link.TextDecorations = null;
            link.TargetName = "@" + Mention.ScreenName;
            link.Click += new RoutedEventHandler(link_Click);

            return link;
        }

        Inline CreateUrlLink(TwitterUrl URL)
        {
            var link = new Hyperlink();
            link.Inlines.Add(new Run() { Text = TweetTextConverter.TrimUrl(URL.ExpandedValue) });
            link.FontWeight = FontWeights.Bold;
            link.TextDecorations = null;
            link.TargetName = URL.ExpandedValue;
            link.Click += new RoutedEventHandler(link_Click);

            return link;
        }

        Inline CreateMediaLink(TwitterMedia Media)
        {
            var link = new Hyperlink();
            link.Inlines.Add(new Run() { Text = Media.DisplayUrl });
            link.FontWeight = FontWeights.Bold;
            link.TextDecorations = null;
            link.TargetName = Media.ExpandedUrl;
            link.Click += new RoutedEventHandler(link_Click);

            return link;
        }

        private void ImageClick(object sender, EventArgs e)
        {
            System.Windows.Controls.Image Img = sender as System.Windows.Controls.Image;
            if(Img != null)
                NavigationService.Navigate(new Uri("/Pages/Elements/ImageView.xaml?img=" + Img.Tag.ToString(), UriKind.Relative));
        }

        void link_Click(object sender, RoutedEventArgs e)
        {
            Hyperlink link = sender as Hyperlink;
            Uri uri;
            WebBrowserTask browser;

            if (link == null || string.IsNullOrWhiteSpace(link.TargetName))
                return;

            if (Uri.TryCreate(link.TargetName, UriKind.Absolute, out uri) ||
                (link.TargetName.StartsWith("www.") && Uri.TryCreate("http://" + link.TargetName, UriKind.Absolute, out uri)))
            {
                browser = new WebBrowserTask();
                browser.Uri = uri;
                browser.Show();
            }
            else if (link.TargetName[0] == '@')
                NavigationService.Navigate(new Uri("/Pages/Elements/User.xaml?user=" + link.TargetName.Substring(0), UriKind.Relative));
            else if (link.TargetName[0] == '#')
            {
                DataTransfer.Search = link.TargetName;
                NavigationService.Navigate(new Uri("/Pages/Search/Search.xaml?q=" + link.TargetName, UriKind.Relative));
            }
              
        }

        private void replyButton_Click(object sender, EventArgs e)
        {
            DataTransfer.ReplyId = status.Id;
            DataTransfer.Text = "@" + status.Author.ScreenName + " ";
            DataTransfer.ReplyingDM = false;
            NavigationService.Navigate(Uris.WriteTweet);
        }

        private void replyAllButton_Click(object sender, EventArgs e)
        {
            DataTransfer.ReplyId = status.Id;
            DataTransfer.Text = "@" + status.Author.ScreenName + " ";
            DataTransfer.ReplyingDM = false;

            foreach (string user in StringManipulator.GetUserNames(status.Text))
                DataTransfer.Text += "@" + user + " ";

            NavigationService.Navigate(Uris.WriteTweet);
        }

        private void retweetButton_Click(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(() => pBar.IsVisible = true);
            ServiceDispatcher.GetCurrentService().Retweet(status.Id, (Action<TwitterStatus, TwitterResponse>)receive);
        }

        private void favButton_Click(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(() => pBar.IsVisible = true);
            if (_favBtnState)
            {
                ServiceDispatcher.GetCurrentService().FavoriteTweet(status.Id, (Action<TwitterStatus, TwitterResponse>)receiveFav);
            }
            else
            {
                ServiceDispatcher.GetCurrentService().UnfavoriteTweet(status.Id, receiveFav);
            }
        }

        private void receive(TwitterStatus status, TwitterResponse resp)
        {
            if (resp.StatusCode != HttpStatusCode.OK)
                Dispatcher.BeginInvoke(() => { MessageBox.Show("An error has occurred :("); });
            Dispatcher.BeginInvoke(() => { pBar.IsVisible = false; Notificator.ShowMessage("Retweeted!", pBar); });
        }

        private void receiveFav(TwitterStatus status, TwitterResponse resp)
        {
            if (resp.StatusCode != HttpStatusCode.OK)
                Dispatcher.BeginInvoke(() => { MessageBox.Show("An error has occurred :("); });
            Dispatcher.BeginInvoke(() => { pBar.IsVisible = false; ToggleFavButton(); Notificator.ShowMessage("Done!", pBar); });
        }

        private void quoteButton_Click(object sender, EventArgs e)
        {
            DataTransfer.ReplyId = 0;
            DataTransfer.Text = "RT @" + status.User.ScreenName + " " + status.Text;

            NavigationService.Navigate(Uris.WriteTweet);
        }

        private void shareButton_Click(object sender, EventArgs e)
        {
            EmailComposeTask emailComposeTask = new EmailComposeTask();

            emailComposeTask.Subject = "Tweet from @" + status.Author.ScreenName;
            emailComposeTask.Body = "@" + status.Author.ScreenName + ": " + status.Text + Environment.NewLine + Environment.NewLine +
                status.CreatedDate.ToString();

            emailComposeTask.Show();
        }

        private void Grid_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            NavigationService.Navigate(new Uri("/Pages/Elements/User.xaml?user=" + status.Author.ScreenName, UriKind.Relative));
        }
        
        private void Replies_Tap(object sender, EventArgs e)
        {
            NavigationService.Navigate(Uris.Conversation);
        }
    }
}